namespace CreditScanAI.Domain.Enums;

/// <summary>
/// Matches the actor/permission matrix in 10_CASOS_DE_USO.md section 5
/// exactly (Analyst/Reviewer/CFO/Admin/Compliance) - superseded the earlier
/// 3-value placeholder (Admin/Analyst/Viewer) once real authentication and
/// role-based authorization were built (Fase 8).
/// </summary>
public enum UserRole
{
    Analyst,
    Reviewer,
    CFO,
    Admin,
    Compliance
}
