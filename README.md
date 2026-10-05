# RMS Leave Management Backend System

## 📋 Overview
Complete backend system for RMS Leave Management with role-based access control, dynamic leave calculation, and comprehensive state machine for request workflow.

## 🏗️ Architecture

### Technology Stack
- **Framework**: ASP.NET Core 10.0
- **Database**: SQL Server
- **ORM**: Entity Framework Core 10.0
- **API Style**: RESTful
- **Documentation**: Swagger/OpenAPI

### Project Structure
```
RMS-BACKEND/
├── Models/              # Entity models matching database schema
├── DTOs/                # Data Transfer Objects for API requests/responses
├── Data/                # DbContext and database configuration
├── Repositories/        # Data access layer
├── Services/            # Business logic layer
├── Controllers/         # API endpoints
└── Program.cs           # Application configuration
```

## 🔐 Roles & Permissions

### Role Determination
Roles are determined by `DepartmentID`:
- **DepartmentID = 10** → HR (ultimate authority)
- **DepartmentID = 11** → Board (ultimate authority)
- **EmployeeRole = Manager** → Manager (team authority)
- **Default** → Employee

### Role-Based Access

#### Board
- Dashboard with filters, stats, and charts
- History of all requests
- HR Requests view
- Profile management

#### HR (Admin)
- Dashboard with filters, stats, and charts
- My Requests (personal)
- Staff Requests (all employees)
- Employee List (add/edit/soft delete)
- **Override Authority**: Can approve/reject any request at any time
- Profile management

#### Manager
- Profile management
- My Requests (personal)
- My Staff Requests (team members)
- Leave Balance Report (dynamic)
- Can approve/reject team requests

#### Employee
- Profile management
- My Requests (editable while Pending)
- Leave Balance Report (dynamic, cannot see past dates)

## 🔄 Request Flow State Machine

### Status Flow
```
Pending (1)
├─ Employee can EDIT
├─ Employee can CANCEL
├─ Manager can APPROVE → Pending HR (2)
├─ Manager can REJECT → Rejected by Manager (4) [FINAL]
└─ HR/Board can APPROVE → Approved by HR (3) [FINAL]
   └─ HR/Board can REJECT → Rejected by HR (5) [FINAL]

Pending HR (2)
├─ Employee CANNOT edit
├─ Employee can CANCEL (until HR decision)
├─ HR can APPROVE → Approved by HR (3) [FINAL]
└─ HR can REJECT → Rejected by HR (5) [FINAL]

Final Statuses:
- Approved by HR (3)
- Rejected by Manager (4)
- Rejected by HR (5)
- Cancelled by Employee (6)
```

### Edit & Cancel Rules
- **Can Edit**: Only while status = Pending (1) and only by request owner
- **Can Cancel**: While Pending (1) or Pending HR (2), before final decision, only by request owner
- **Cannot Edit/Cancel**: After final decision (statuses 3, 4, 5, 6)

### HR Override
- HR can approve/reject ANY request at ANY time (except final statuses)
- HR decision overrides Manager decisions
- Once HR decides → Status becomes FINAL

## 📊 Leave Balance Calculation

### Formula
```
LeaveDaysUsed = Unit × Sign × (EndDate - StartDate).Days
```

### Parameters
- **Unit**: 
  - 1 = Full day
  - 0.5 = Half day
  - 0.25 = Quarter day
- **Sign**:
  - +1 = Addition (e.g., bonus leave)
  - -1 = Deduction (e.g., leave taken)
  - 0 = Neutral (e.g., sick leave)

### Employee Levels
- **Level A**: 15 days/year
- **Level B**: 24 days/year

### Accrual Rules
- **Monthly Accrual**: Total Annual Leave / 12
- **Probation Period**: No accrual for first 6 months
- **Carryover**: Reserved field; the owner must define the amount, cap, and eligibility before calculation is enabled
- **Expiration**: Not implemented until the carryover policy defines the expiry window

### Dynamic Calculation
Leave balance is calculated dynamically based on:
1. Date of employment
2. Months of service (excluding probation)
3. Employee level entitlement
4. Approved leave transactions
5. Current date or specified "as of" date

## 📈 Dashboard & Charts

### Board & HR Dashboards
- **Bar Chart**: Requests per Department/Employee
- **Pie Chart**: Distribution by Department/Employee
- **Filters**: Status, Department, Employee, Date Range
- **Group By**: Department or Employee
- **Stats**: Total, Pending, Approved, Rejected, Cancelled

### Employee Dashboard
- Shows only personal requests
- Filters apply to own requests
- Charts show personal stats
- No department grouping

### Manager Dashboard
- Shows team requests
- Filters apply to team members
- Grouping by Employee or Department
- Team statistics

## 🔌 API Endpoints

### Authentication
- `POST /api/auth/login` - Login and get role information

### Transactions (Leave Requests)
- `POST /api/transactions` - Create new request
- `PUT /api/transactions/{id}` - Update pending request
- `POST /api/transactions/{id}/cancel` - Cancel request
- `POST /api/transactions/{id}/approve` - Approve request (Manager/HR/Board)
- `POST /api/transactions/{id}/reject` - Reject request (Manager/HR/Board)
- `GET /api/transactions/{id}` - Get specific request
- `GET /api/transactions/my-requests?page=1&pageSize=100` - Get a bounded page of my requests
- `GET /api/transactions/my-team-requests?page=1&pageSize=100` - Get a bounded page of team requests (Manager)
- `GET /api/transactions/all?page=1&pageSize=100` - Get a bounded page of all requests (HR/Board)
- `POST /api/transactions/filter?page=1&pageSize=100` - Get a bounded filtered page; `pageSize` is capped at 200

### Dashboard
- `POST /api/dashboard/stats` - Get dashboard statistics
- `POST /api/dashboard/charts` - Get chart data (bar & pie)

### Leave Balance
- `GET /api/leavebalance/{employeeId}` - Get employee balance
- `GET /api/leavebalance/my-balance` - Get my balance
- `GET /api/leavebalance/team-balances` - Get team balances (Manager)
- `GET /api/leavebalance/department/{departmentId}` - Get department balances
- `GET /api/leavebalance/all` - Get all balances (HR/Board)

### Employees (HR Only)
- `GET /api/employees` - Get all employees
- `GET /api/employees/{id}` - Get employee by ID
- `POST /api/employees` - Create new employee
- `PUT /api/employees/{id}` - Update employee
- `DELETE /api/employees/{id}` - Soft delete employee

## 🗄️ Database Schema

### Tables
1. **Employees** - Employee information
2. **EmployeeLevel** - Leave entitlement levels (A, B)
3. **Status** - Transaction statuses and departments
4. **TransactionTypes** - Leave types with Unit and Sign
5. **Transactions** - Leave requests

### Key Relationships
- Employee → EmployeeLevel (many-to-one)
- Employee → Employee (Manager, self-referencing)
- Employee → Status (Department, many-to-one)
- Transaction → Employee (many-to-one)
- Transaction → TransactionType (many-to-one)
- Transaction → Status (many-to-one)

## 🚀 Getting Started

### Prerequisites
- .NET 10.0 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### Configuration
1. Update connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Running the Application
```bash
# Restore and build from the repository root
dotnet restore RMS-BACKEND.slnx
dotnet build RMS-BACKEND.slnx --no-restore

# Run the HTTP development profile
dotnet run --project RMS-BACKEND/RMS-BACKEND.csproj --no-build --launch-profile http
```

The API will be available at:
- **HTTP**: http://localhost:5190
- **HTTPS**: https://localhost:7167
- **Swagger UI**: http://localhost:5190 (Development, HTTP profile)
- **Frontend**: http://localhost:5173/login; see HOW_TO_RUN.md for the verified launch commands

## 📝 Usage Examples

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "code": "1980009",
  "password": "<current-test-password>"
}
```

### Create Leave Request
```http
POST /api/transactions
Authorization: Bearer <session-token>
Content-Type: application/json

{
  "transactionTypesID": 2,
  "startDate": "2026-03-01",
  "endDate": "2026-03-05",
  "substituteEmployeeId": 3,
  "leaveRationale": "Family vacation"
}
```

### Approve Request (Manager/HR)
```http
POST /api/transactions/1/approve
Authorization: Bearer <session-token>
Content-Type: application/json

{
  "transactionId": 1,
  "responseMessage": "Approved"
}
```

### Get Dashboard Stats
```http
POST /api/dashboard/stats
Authorization: Bearer <session-token>
Content-Type: application/json

{
  "departmentID": 7,
  "groupBy": "Department"
}
```

## 🔧 Key Features

### ✅ Implemented
- [x] Complete entity models matching database schema
- [x] Role-based access control (Board, HR, Manager, Employee)
- [x] Request state machine with edit/cancel rules
- [x] HR override authority
- [x] Dynamic leave balance calculation
- [x] Probation period handling (6 months)
- [x] Monthly leave accrual
- [x] Dashboard with filtering and grouping
- [x] Bar and pie chart data generation
- [x] Transaction CRUD operations
- [x] Employee management (HR)
- [x] Leave balance reports
- [x] Audit trail (CreationDate, ResponseDate)

### 🔄 Future Enhancements
- [ ] Carryover calculation after the owner approves cap/eligibility/expiry rules
- [ ] Email notifications
- [ ] Advanced reporting and exports
- [ ] Password reset/onboarding workflow
- [ ] Full audit log for non-decision changes

## 📖 Business Rules Summary

1. **Flow Determination**: Based on Employee's DepartmentID
2. **Edit Permission**: Only while Pending, only by owner
3. **Cancel Permission**: Before final decision, only by owner
4. **Manager Authority**: Can approve/reject team requests (Pending → Pending HR or Rejected)
5. **HR Authority**: Can approve/reject ANY request at ANY time (override)
6. **Final Statuses**: No further changes allowed
7. **Leave Calculation**: Unit × Sign × Days
8. **Probation**: No leave accrual for first 6 months
9. **Accrual**: Monthly = Annual / 12
10. **Balance**: Dynamic calculation based on approved transactions

## 🎯 Testing Recommendations

1. Test role determination for each department
2. Test state transitions for all roles
3. Test edit/cancel permissions at each status
4. Test HR override functionality
5. Test leave balance calculation with various scenarios
6. Test dashboard filtering and grouping
7. Test probation period calculations
8. Test manager team request visibility

## 📞 Support

For issues or questions, refer to the API documentation at the Swagger UI endpoint when running in Development mode.

---

**Version**: 1.0.0  
**Last Updated**: 2026-02-02  
**Author**: RMS Backend Expert AI
