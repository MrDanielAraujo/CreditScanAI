using System.Security.Cryptography;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Documents;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Domain.Utils;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB, per 10_CASOS_DE_USO.md UC-02
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    private readonly AppDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly IDocumentProcessingQueue _queue;
    private readonly IClassificationProcessingQueue _classificationQueue;

    public DocumentsController(
        AppDbContext db, IDocumentStorage storage, IDocumentProcessingQueue queue, IClassificationProcessingQueue classificationQueue)
    {
        _db = db;
        _storage = storage;
        _queue = queue;
        _classificationQueue = classificationQueue;
    }

    /// <summary>
    /// Gerenciamento de Documentos (Fase 7): lista todos os documentos já
    /// enviados, com filtros - não existia nenhuma tela para isso antes, só
    /// o fluxo de Upload em si.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DocumentListResponse>>> List(
        [FromQuery] Guid? companyId,
        [FromQuery] string? documentType,
        [FromQuery] string? search,
        [FromQuery] int limit,
        [FromQuery] int offset,
        CancellationToken cancellationToken)
    {
        limit = limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);
        offset = Math.Max(offset, 0);

        var query = _db.Documents.AsQueryable();

        if (companyId is not null)
        {
            query = query.Where(d => d.CompanyId == companyId);
        }

        if (!string.IsNullOrWhiteSpace(documentType) && Enum.TryParse<DocumentType>(documentType, ignoreCase: true, out var parsedType))
        {
            query = query.Where(d => d.DocumentType == parsedType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => d.FileName.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = await query
            .OrderByDescending(d => d.UploadDate)
            .Skip(offset)
            .Take(limit)
            .Select(d => new
            {
                d.Id,
                d.FileName,
                d.CompanyId,
                CompanyName = d.Company!.Name,
                d.DocumentType,
                d.UploadDate,
                d.ExtractionStatus,
                d.ClassificationStatus
            })
            .ToListAsync(cancellationToken);

        var items = page
            .Select(d => new DocumentListItemDto(
                d.Id, d.FileName, d.CompanyId, d.CompanyName, d.DocumentType?.ToString(),
                d.UploadDate, d.ExtractionStatus.ToString(), d.ClassificationStatus.ToString()))
            .ToList();

        return Ok(ApiResponse<DocumentListResponse>.Ok(new DocumentListResponse(items, totalCount, offset + items.Count < totalCount)));
    }

    /// <summary>
    /// Reclassifica um documento já extraído do zero (Fase 7) - fecha uma
    /// lacuna real: hoje, se o plano de contas muda depois do upload
    /// (ex: novas Contas Padrão cadastradas), a classificação antiga nunca é
    /// refeita automaticamente. Só reprocessa classificação, não extração -
    /// a estrutura/hierarquia do documento não muda, só quais Contas Padrão
    /// estão disponíveis para casar com ela. ClassificationProcessingService
    /// já atualiza (não duplica) as classificações existentes.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.CanUpload)]
    [HttpPost("{id:guid}/reprocess")]
    public async Task<ActionResult<ApiResponse<ReprocessDocumentResponse>>> Reprocess(Guid id, CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Documento não encontrado."));
        }

        if (document.ExtractionStatus != ExtractionStatus.Completed)
        {
            return Conflict(ApiResponse<object>.Fail(
                "NOT_EXTRACTED", $"Documento ainda não foi extraído com sucesso (status atual: {document.ExtractionStatus})."));
        }

        _classificationQueue.Enqueue(id);

        return Accepted(ApiResponse<ReprocessDocumentResponse>.Ok(
            new ReprocessDocumentResponse(id, document.ClassificationStatus.ToString())));
    }

    /// <summary>
    /// O tipo do documento não é mais pedido aqui - é derivado automaticamente
    /// depois da extração, a partir das contas que o sistema classifica por
    /// palavra-chave (Balanço, DRE, ou os dois juntos). No lugar de escolher
    /// uma empresa já cadastrada, o chamador informa o CNPJ: se não houver
    /// empresa com esse CNPJ (dentro do único tenant existente), uma nova é
    /// criada na hora com um nome provisório, que o processamento em segundo
    /// plano tenta substituir pelo nome de verdade achado perto do CNPJ no
    /// texto do PDF - o CNPJ é o dado que importa, o resto é best-effort.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.CanUpload)]
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<ApiResponse<UploadDocumentResponse>>> Upload(
        IFormFile file,
        [FromForm] string cnpj,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Arquivo vazio."));
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Tamanho máximo de 100MB excedido."));
        }

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Apenas arquivos PDF são aceitos."));
        }

        if (string.IsNullOrWhiteSpace(cnpj) || !CnpjValidator.IsValid(cnpj))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "CNPJ inválido."));
        }

        var tenantId = await _db.Tenants.OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstOrDefaultAsync(cancellationToken);
        if (tenantId == Guid.Empty)
        {
            return Conflict(ApiResponse<object>.Fail("NO_TENANT", "Nenhum tenant cadastrado."));
        }

        var cnpjDigits = CnpjValidator.OnlyDigits(cnpj);
        var tenantCompaniesWithCnpj = await _db.Companies
            .Where(c => c.TenantId == tenantId && c.Cnpj != null)
            .ToListAsync(cancellationToken);
        var company = tenantCompaniesWithCnpj.FirstOrDefault(c => CnpjValidator.OnlyDigits(c.Cnpj!) == cnpjDigits);
        var companyCreated = false;

        if (company is null)
        {
            company = new Company
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = cnpjDigits,
                Name = CnpjValidator.PlaceholderCompanyName(cnpj),
                Cnpj = CnpjValidator.Format(cnpj),
                ReportingCurrency = "BRL",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync(cancellationToken);
            companyCreated = true;
        }

        var documentId = Guid.NewGuid();

        string fileHash;
        await using (var hashStream = file.OpenReadStream())
        {
            fileHash = Convert.ToHexString(await SHA256.HashDataAsync(hashStream, cancellationToken));
        }

        await using var saveStream = file.OpenReadStream();
        var filePath = await _storage.SaveAsync(tenantId, documentId, saveStream, cancellationToken);

        var document = new Document
        {
            Id = documentId,
            TenantId = tenantId,
            CompanyId = company.Id,
            DocumentType = null,
            UploadDate = DateTime.UtcNow,
            FileName = file.FileName,
            FilePath = filePath,
            FileSize = file.Length,
            FileHash = fileHash,
            ExtractionStatus = ExtractionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        _queue.Enqueue(documentId);

        return Accepted(ApiResponse<UploadDocumentResponse>.Ok(
            new UploadDocumentResponse(documentId, document.ExtractionStatus.ToString(), company.Id, companyCreated)));
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<DocumentStatusResponse>>> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Documento não encontrado."));
        }

        return Ok(ApiResponse<DocumentStatusResponse>.Ok(new DocumentStatusResponse(
            document.Id,
            document.ExtractionStatus.ToString(),
            document.ExtractionStartedAt,
            document.ExtractionCompletedAt,
            document.ExtractionError)));
    }

    [HttpGet("{id:guid}/result")]
    public async Task<ActionResult<ApiResponse<DocumentResultResponse>>> GetResult(Guid id, CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Documento não encontrado."));
        }

        if (document.ExtractionStatus != ExtractionStatus.Completed)
        {
            return Conflict(ApiResponse<object>.Fail("NOT_READY", $"Documento ainda está com status {document.ExtractionStatus}."));
        }

        var accounts = await _db.SourceAccounts
            .Where(a => a.DocumentId == id)
            .ToListAsync(cancellationToken);

        var values = await _db.AccountValues
            .Where(v => v.DocumentId == id)
            .ToListAsync(cancellationToken);

        var periodIds = values.Where(v => v.PeriodId.HasValue).Select(v => v.PeriodId!.Value).Distinct().ToList();
        var periods = await _db.Periods
            .Where(p => periodIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var valuesByAccount = values.ToLookup(v => v.SourceAccountId);

        var accountDtos = accounts.Select(a => new SourceAccountDto(
            a.Id,
            a.ParentSourceAccountId,
            a.OriginalName,
            a.HierarchyLevel,
            a.InferredType,
            a.InferredSubtype,
            valuesByAccount[a.Id]
                .Select(v => new AccountValueDto(v.PeriodId, v.RawColumnLabel, v.RawValue, v.ScaleFactor))
                .ToList())).ToList();

        var periodDtos = periods
            .Select(p => new PeriodDto(p.Id, p.PeriodType.ToString(), p.Year, p.Quarter, p.EndDate))
            .ToList();

        return Ok(ApiResponse<DocumentResultResponse>.Ok(
            new DocumentResultResponse(document.Id, document.ExtractionStatus.ToString(), periodDtos, accountDtos)));
    }
}
