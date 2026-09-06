namespace CreditScanAI.Api.Services;

/// <summary>
/// Matches the "Upload/Review/Consolidate/Admin" columns of the permission
/// matrix in 10_CASOS_DE_USO.md section 5 (Fase 8 Parte 2). "Export"
/// (read-only) and "Login" have no policy of their own - any authenticated
/// user already satisfies them via the app's fallback authorization policy.
/// </summary>
public static class AuthorizationPolicies
{
    public const string CanUpload = "CanUpload";
    public const string CanReview = "CanReview";
    public const string CanConsolidate = "CanConsolidate";
    public const string CanAdmin = "CanAdmin";
}
