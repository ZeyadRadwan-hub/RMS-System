# RMS medical document persistence

Scope: APP-BT-011/053 and any proven storage/access bug in the same workflow.

1. Test the current sick-leave flow with a synthetic PDF: a request without a document must fail, multipart upload must persist bytes, and only owner/direct manager/HR/Board may fetch them. Test invalid MIME/magic, extension, count, and size with no surviving transaction.
2. Back up and verify authorized `RMS`, then add a versioned additive `MedicalDocuments` table with FK to Transactions and Employees, SHA-256, content, size, MIME, filename, timestamps and indexes. Provide an explicit history-preserving rollback guard.
3. Accept `multipart/form-data` for sick leave while preserving JSON for other types. Validate file count/size/signature server-side before write. Save transaction and documents in one DB transaction. Reject sick leave without a valid document. Keep existing historical sick requests unchanged.
4. Add metadata/download endpoints with the same authorization scope as transaction-by-ID, do not expose content in list DTOs. Make frontend send FormData with actual `File` bytes and provide a retrieval path.
5. Verify negative cases, bytes round-trip, transaction counts/history and frontend build/browser flow. Do not claim closed if the file path or access tests fail.
