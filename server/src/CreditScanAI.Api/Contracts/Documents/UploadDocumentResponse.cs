namespace CreditScanAI.Api.Contracts.Documents;

public sealed record UploadDocumentResponse(Guid DocumentId, string Status, Guid CompanyId, bool CompanyCreated);
