namespace CreditScanAI.Classification;

/// <summary>Um padrão aprendido a partir de decisões humanas em outras empresas do mesmo tenant.</summary>
public sealed record CrossCompanyPattern(Guid StandardAccountId, string StandardAccountName, int CompanyCount, float Consistency);

/// <summary>
/// Camada de aprendizado entre empresas (Fase 6 Parte 1): quando várias
/// empresas diferentes do mesmo tenant já classificaram (e um humano
/// aprovou/corrigiu) o mesmo nome normalizado de conta para a mesma conta
/// padrão, isso é um sinal de que o padrão generaliza - útil justamente para
/// uma empresa nova, que ainda não tem histórico próprio (Camada 4, que só
/// olha dentro da mesma empresa). Implementado na camada Api (precisa de
/// acesso ao banco) - este projeto permanece agnóstico de EF/banco.
/// </summary>
public interface ICrossCompanyPatternProvider
{
    Task<CrossCompanyPattern?> FindPatternAsync(
        Guid tenantId,
        Guid excludeCompanyId,
        string normalizedSourceAccountName,
        CancellationToken cancellationToken);
}
