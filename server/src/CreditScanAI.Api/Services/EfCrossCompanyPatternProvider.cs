using CreditScanAI.Classification;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Services;

/// <summary>
/// Busca, entre empresas do mesmo tenant (exceto a que está sendo
/// classificada agora), decisões humanas confirmadas (Approved/Overridden)
/// para o mesmo nome normalizado de conta. Cada empresa conta como um único
/// "voto" (a mais recente entre suas próprias decisões) - vários uploads da
/// mesma empresa não devem pesar mais que uma empresa diferente. Só vira um
/// padrão aceito com pelo menos <see cref="MinDistinctCompanies"/> empresas
/// distintas concordando em pelo menos <see cref="MinConsistency"/> dos casos.
/// </summary>
public class EfCrossCompanyPatternProvider : ICrossCompanyPatternProvider
{
    private const int MinDistinctCompanies = 2;
    private const float MinConsistency = 0.8f;

    private readonly AppDbContext _db;

    public EfCrossCompanyPatternProvider(AppDbContext db) => _db = db;

    public async Task<CrossCompanyPattern?> FindPatternAsync(
        Guid tenantId,
        Guid excludeCompanyId,
        string normalizedSourceAccountName,
        CancellationToken cancellationToken)
    {
        var decisions = await (
            from classification in _db.AccountClassifications
            join sourceAccount in _db.SourceAccounts on classification.SourceAccountId equals sourceAccount.Id
            join document in _db.Documents on sourceAccount.DocumentId equals document.Id
            where document.TenantId == tenantId
                && document.CompanyId != excludeCompanyId
                && sourceAccount.NormalizedName == normalizedSourceAccountName
                && classification.StandardAccountId != null
                && (classification.ReviewStatus == ClassificationReviewStatus.Approved
                    || classification.ReviewStatus == ClassificationReviewStatus.Overridden)
            orderby (classification.ReviewedAt ?? classification.UpdatedAt)
            select new { document.CompanyId, classification.StandardAccountId }
        ).ToListAsync(cancellationToken);

        var votePerCompany = decisions
            .GroupBy(d => d.CompanyId)
            .Select(g => g.Last().StandardAccountId!.Value)
            .ToList();

        if (votePerCompany.Count < MinDistinctCompanies)
        {
            return null;
        }

        var winner = votePerCompany
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .First();

        var consistency = (float)winner.Count() / votePerCompany.Count;
        if (consistency < MinConsistency)
        {
            return null;
        }

        var standardAccount = await _db.StandardAccounts.FirstAsync(a => a.Id == winner.Key, cancellationToken);

        return new CrossCompanyPattern(standardAccount.Id, standardAccount.Name, votePerCompany.Count, consistency);
    }
}
