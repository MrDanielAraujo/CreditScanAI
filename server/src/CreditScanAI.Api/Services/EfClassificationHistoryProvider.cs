using CreditScanAI.Classification;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Services;

/// <summary>
/// Busca, para a mesma empresa, a classificação mais recente que já foi
/// validada por um humano (Approved/Overridden) para o mesmo nome
/// normalizado de conta - em qualquer documento exceto o que está sendo
/// processado agora.
/// </summary>
public class EfClassificationHistoryProvider : IClassificationHistoryProvider
{
    private readonly AppDbContext _db;

    public EfClassificationHistoryProvider(AppDbContext db) => _db = db;

    public async Task<HistoricalClassification?> FindPreviousDecisionAsync(
        Guid companyId,
        Guid excludeDocumentId,
        string normalizedSourceAccountName,
        CancellationToken cancellationToken)
    {
        var match = await (
            from classification in _db.AccountClassifications
            join sourceAccount in _db.SourceAccounts on classification.SourceAccountId equals sourceAccount.Id
            join document in _db.Documents on sourceAccount.DocumentId equals document.Id
            join standardAccount in _db.StandardAccounts on classification.StandardAccountId equals standardAccount.Id
            where document.CompanyId == companyId
                && document.Id != excludeDocumentId
                && sourceAccount.NormalizedName == normalizedSourceAccountName
                && (classification.ReviewStatus == ClassificationReviewStatus.Approved
                    || classification.ReviewStatus == ClassificationReviewStatus.Overridden)
            orderby (classification.ReviewedAt ?? classification.UpdatedAt) descending
            select new { standardAccount.Id, standardAccount.Name, Decided = classification.ReviewedAt ?? classification.UpdatedAt }
        ).FirstOrDefaultAsync(cancellationToken);

        return match is null ? null : new HistoricalClassification(match.Id, match.Name, match.Decided);
    }
}
