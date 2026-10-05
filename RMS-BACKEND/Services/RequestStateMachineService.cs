namespace RMS_BACKEND.Services
{
    /// <summary>
    /// Transaction Status Constants
    /// </summary>
    public static class TransactionStatus
    {
        public const int Pending = 1;
        public const int PendingHR = 2;
        public const int ApprovedByHR = 3;
        public const int RejectedByManager = 4;
        public const int RejectedByHR = 5;
        public const int CancelledByEmployee = 6;
    }

    /// <summary>
    /// Department Constants
    /// </summary>
    public static class DepartmentConstants
    {
        public const int Quality = 7;
        public const int Marketing = 8;
        public const int Finance = 9;
        public const int HR = 10;
        public const int Board = 11;
    }

    public interface IRequestStateMachineService
    {
        bool CanEdit(int currentStatusId, int employeeId, int requestOwnerId);
        bool CanCancel(int currentStatusId, int employeeId, int requestOwnerId);
        bool CanApprove(int currentStatusId, string userRole, int userDepartmentId, int? requestOwnerManagerId, int approverId);
        bool CanReject(int currentStatusId, string userRole, int userDepartmentId, int? requestOwnerManagerId, int approverId);
        int GetNextStatusOnApproval(int currentStatusId, string userRole);
        int GetNextStatusOnRejection(int currentStatusId, string userRole);
        bool IsFinalStatus(int statusId);
        string DetermineUserRole(int departmentId, bool isManager);
    }

    public class RequestStateMachineService : IRequestStateMachineService
    {
        /// <summary>
        /// Employee can edit only while status is Pending
        /// </summary>
        public bool CanEdit(int currentStatusId, int employeeId, int requestOwnerId)
        {
            // Only the request owner can edit
            if (employeeId != requestOwnerId)
                return false;

            // Can only edit while Pending
            return currentStatusId == TransactionStatus.Pending;
        }

        /// <summary>
        /// Employee can cancel before final decision
        /// Cannot cancel if: Rejected by Manager, Rejected by HR, Approved by HR, or already Cancelled
        /// </summary>
        public bool CanCancel(int currentStatusId, int employeeId, int requestOwnerId)
        {
            // Only the request owner can cancel
            if (employeeId != requestOwnerId)
                return false;

            // Cannot cancel if already in final status
            if (IsFinalStatus(currentStatusId))
                return false;

            // Can cancel while Pending or Pending HR
            return currentStatusId == TransactionStatus.Pending || 
                   currentStatusId == TransactionStatus.PendingHR;
        }

        /// <summary>
        /// Determines if user can approve based on role and current status
        /// </summary>
        public bool CanApprove(int currentStatusId, string userRole, int userDepartmentId, int? requestOwnerManagerId, int approverId)
        {
            // HR can approve at any time (override)
            if (userRole == "HR")
                return !IsFinalStatus(currentStatusId);

            // Board can approve at any time (override)
            if (userRole == "Board")
                return !IsFinalStatus(currentStatusId);

            // Manager can only approve Pending requests from their team
            if (userRole == "Manager" && currentStatusId == TransactionStatus.Pending)
                return requestOwnerManagerId.HasValue && requestOwnerManagerId.Value == approverId;

            return false;
        }

        /// <summary>
        /// Determines if user can reject based on role and current status
        /// </summary>
        public bool CanReject(int currentStatusId, string userRole, int userDepartmentId, int? requestOwnerManagerId, int approverId)
        {
            // HR can reject at any time (override)
            if (userRole == "HR")
                return !IsFinalStatus(currentStatusId);

            // Board can reject at any time (override)
            if (userRole == "Board")
                return !IsFinalStatus(currentStatusId);

            // Manager can only reject Pending requests from their team
            if (userRole == "Manager" && currentStatusId == TransactionStatus.Pending)
                return requestOwnerManagerId.HasValue && requestOwnerManagerId.Value == approverId;

            return false;
        }

        /// <summary>
        /// Gets next status when request is approved
        /// </summary>
        public int GetNextStatusOnApproval(int currentStatusId, string userRole)
        {
            // HR/Board approval is final
            if (userRole == "HR" || userRole == "Board")
                return TransactionStatus.ApprovedByHR;

            // Manager approval moves to Pending HR
            if (userRole == "Manager" && currentStatusId == TransactionStatus.Pending)
                return TransactionStatus.PendingHR;

            return currentStatusId; // No change
        }

        /// <summary>
        /// Gets next status when request is rejected
        /// </summary>
        public int GetNextStatusOnRejection(int currentStatusId, string userRole)
        {
            // HR/Board rejection is final
            if (userRole == "HR" || userRole == "Board")
                return TransactionStatus.RejectedByHR;

            // Manager rejection is final
            if (userRole == "Manager" && currentStatusId == TransactionStatus.Pending)
                return TransactionStatus.RejectedByManager;

            return currentStatusId; // No change
        }

        /// <summary>
        /// Checks if status is final (no further changes allowed)
        /// </summary>
        public bool IsFinalStatus(int statusId)
        {
            return statusId == TransactionStatus.ApprovedByHR ||
                   statusId == TransactionStatus.RejectedByManager ||
                   statusId == TransactionStatus.RejectedByHR ||
                   statusId == TransactionStatus.CancelledByEmployee;
        }

        /// <summary>
        /// Determines user role based on DepartmentID
        /// Flow determination: StatusID = 10 → HR, StatusID = 11 → Board
        /// </summary>
        public string DetermineUserRole(int departmentId, bool isManager)
        {
            if (departmentId == DepartmentConstants.HR)
                return "HR";

            if (departmentId == DepartmentConstants.Board)
                return "Board";

            if (isManager)
                return "Manager";

            return "Employee";
        }
    }
}
