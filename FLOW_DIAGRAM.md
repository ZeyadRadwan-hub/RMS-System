# RMS Leave Management - State Diagram & Flow Documentation

## 📊 Request Lifecycle State Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         EMPLOYEE SUBMITS REQUEST                         │
│                                    ↓                                     │
│                            ┌──────────────┐                              │
│                            │   PENDING    │ (Status ID: 1)               │
│                            │              │                              │
│                            │ ✓ Can Edit   │                              │
│                            │ ✓ Can Cancel │                              │
│                            └──────┬───────┘                              │
│                                   │                                      │
│                    ┌──────────────┼──────────────┐                       │
│                    │              │              │                       │
│              ┌─────▼─────┐  ┌────▼────┐  ┌──────▼──────┐               │
│              │ MANAGER   │  │   HR    │  │   EMPLOYEE  │               │
│              │ APPROVES  │  │ OVERRIDE│  │   CANCELS   │               │
│              └─────┬─────┘  └────┬────┘  └──────┬──────┘               │
│                    │             │              │                       │
│              ┌─────▼─────┐       │         ┌────▼─────┐                │
│              │ PENDING   │       │         │CANCELLED │ (Status ID: 6) │
│              │    HR     │       │         │    BY    │                │
│              │           │       │         │ EMPLOYEE │ [FINAL]        │
│              │ ✗ No Edit │       │         └──────────┘                │
│              │ ✓ Cancel  │       │                                     │
│              └─────┬─────┘       │                                     │
│                    │             │                                     │
│         ┌──────────┼─────────────┼─────────────┐                       │
│         │          │             │             │                       │
│    ┌────▼────┐ ┌──▼──────┐ ┌────▼────┐  ┌─────▼──────┐              │
│    │   HR    │ │   HR    │ │ MANAGER │  │  EMPLOYEE  │              │
│    │APPROVES │ │ REJECTS │ │ REJECTS │  │  CANCELS   │              │
│    └────┬────┘ └──┬──────┘ └────┬────┘  └─────┬──────┘              │
│         │         │             │             │                       │
│    ┌────▼────┐ ┌──▼──────┐ ┌────▼─────┐  ┌────▼─────┐               │
│    │APPROVED │ │REJECTED │ │ REJECTED │  │CANCELLED │               │
│    │  BY HR  │ │  BY HR  │ │    BY    │  │    BY    │               │
│    │         │ │         │ │  MANAGER │  │ EMPLOYEE │               │
│    │[FINAL]  │ │[FINAL]  │ │ [FINAL]  │  │ [FINAL]  │               │
│    └─────────┘ └─────────┘ └──────────┘  └──────────┘               │
│   (Status: 3)  (Status: 5)  (Status: 4)   (Status: 6)                │
└─────────────────────────────────────────────────────────────────────────┘
```

## 🔄 Detailed Status Transitions

### Status 1: Pending
**Description**: Initial state when employee submits request

**Employee Actions**:
- ✅ **Edit**: Can modify all request details
- ✅ **Cancel**: Can cancel the request

**Manager Actions**:
- ✅ **Approve**: Moves to Status 2 (Pending HR)
- ✅ **Reject**: Moves to Status 4 (Rejected by Manager) - FINAL

**HR/Board Actions**:
- ✅ **Approve**: Moves to Status 3 (Approved by HR) - FINAL (Override)
- ✅ **Reject**: Moves to Status 5 (Rejected by HR) - FINAL (Override)

---

### Status 2: Pending HR
**Description**: Manager approved, awaiting HR decision

**Employee Actions**:
- ❌ **Edit**: Cannot edit
- ✅ **Cancel**: Can cancel until HR decides

**Manager Actions**:
- ❌ No further actions (already approved)

**HR/Board Actions**:
- ✅ **Approve**: Moves to Status 3 (Approved by HR) - FINAL
- ✅ **Reject**: Moves to Status 5 (Rejected by HR) - FINAL

---

### Status 3: Approved by HR
**Description**: Final approval - request is approved

**All Actions**:
- ❌ **Edit**: Cannot edit
- ❌ **Cancel**: Cannot cancel
- ❌ **Approve/Reject**: No further actions

**Status**: FINAL ✓

---

### Status 4: Rejected by Manager
**Description**: Manager rejected the request

**All Actions**:
- ❌ **Edit**: Cannot edit
- ❌ **Cancel**: Cannot cancel
- ❌ **Approve/Reject**: No further actions

**Status**: FINAL ✗

**Note**: HR can still override if needed before this status is set

---

### Status 5: Rejected by HR
**Description**: HR rejected the request

**All Actions**:
- ❌ **Edit**: Cannot edit
- ❌ **Cancel**: Cannot cancel
- ❌ **Approve/Reject**: No further actions

**Status**: FINAL ✗

---

### Status 6: Cancelled by Employee
**Description**: Employee cancelled the request

**All Actions**:
- ❌ **Edit**: Cannot edit
- ❌ **Cancel**: Already cancelled
- ❌ **Approve/Reject**: No further actions

**Status**: FINAL ⊗

---

## 🎭 Role-Based Flow Determination

### Flow Decision Tree
```
Employee Login
    │
    ├─ Get Employee.DepartmentID
    │
    ├─ IF DepartmentID = 10 → HR Flow
    │   │
    │   ├─ Can view: All Requests
    │   ├─ Can approve/reject: ANY request at ANY time
    │   ├─ Override: Manager decisions
    │   └─ Tabs: Dashboard, My Requests, Staff Requests, Employee List, Profile
    │
    ├─ IF DepartmentID = 11 → Board Flow
    │   │
    │   ├─ Can view: All Requests
    │   ├─ Can approve/reject: ANY request at ANY time
    │   ├─ Override: Manager decisions
    │   └─ Tabs: Dashboard, History, HR Requests, Profile
    │
    ├─ IF EmployeeRole = Manager → Manager Flow
    │   │
    │   ├─ Can view: Team Requests + Own Requests
    │   ├─ Can approve/reject: Team requests (Pending only)
    │   ├─ Cannot override: HR decisions
    │   └─ Tabs: Profile, My Requests, My Staff Requests, Leave Balance
    │
    └─ ELSE → Employee Flow
        │
        ├─ Can view: Own Requests only
        ├─ Can edit: Own Pending requests
        ├─ Can cancel: Own requests (before final)
        └─ Tabs: Profile, My Requests, Leave Balance Report
```

## 📋 Tab Navigation by Role

### Board Role
```
┌─────────────────────────────────────────┐
│ 📊 Dashboard                            │
│    - Filters (Status, Dept, Employee)  │
│    - Stats (Total, Pending, etc.)      │
│    - Bar Chart (Dept/Employee)         │
│    - Pie Chart (Distribution)          │
├─────────────────────────────────────────┤
│ 📜 History                              │
│    - All requests                       │
│    - Filter by Employee Code           │
│    - Filter by Status                  │
│    - Filter by Date Range              │
├─────────────────────────────────────────┤
│ 🏢 HR Requests                          │
│    - All HR-related requests           │
├─────────────────────────────────────────┤
│ 👤 Profile                              │
│    - Personal information              │
└─────────────────────────────────────────┘
```

### HR Role
```
┌─────────────────────────────────────────┐
│ 📊 Dashboard                            │
│    - Filters + Stats + Charts          │
├─────────────────────────────────────────┤
│ 📝 My Requests                          │
│    - Personal leave requests           │
├─────────────────────────────────────────┤
│ 👥 Staff Requests                       │
│    - All employee requests             │
│    - Approve/Reject any request        │
│    - Override authority                │
├─────────────────────────────────────────┤
│ 📋 Employee List                        │
│    - View all employees                │
│    - Add new employee                  │
│    - Edit employee                     │
│    - Soft delete employee              │
│    - Filters (Code, Name, Dept)        │
├─────────────────────────────────────────┤
│ 👤 Profile                              │
│    - Personal information              │
└─────────────────────────────────────────┘
```

### Manager Role
```
┌─────────────────────────────────────────┐
│ 👤 Profile                              │
│    - Personal information              │
├─────────────────────────────────────────┤
│ 📝 My Requests                          │
│    - Personal leave requests           │
│    - Edit while Pending                │
│    - Cancel before final               │
├─────────────────────────────────────────┤
│ 👥 My Staff Requests                    │
│    - Team member requests              │
│    - Approve/Reject (Pending only)     │
│    - View all team requests            │
├─────────────────────────────────────────┤
│ 📊 Leave Balance Report                 │
│    - Team leave balances               │
│    - Dynamic calculation               │
│    - Filter by date                    │
└─────────────────────────────────────────┘
```

### Employee Role
```
┌─────────────────────────────────────────┐
│ 👤 Profile                              │
│    - Personal information              │
├─────────────────────────────────────────┤
│ 📝 My Requests                          │
│    - Personal leave requests           │
│    - Edit while Pending                │
│    - Cancel before final               │
│    - View status and history           │
├─────────────────────────────────────────┤
│ 📊 Leave Balance Report                 │
│    - Personal leave balance            │
│    - Dynamic calculation               │
│    - Cannot see past dates             │
│    - Shows accrual and usage           │
└─────────────────────────────────────────┘
```

## 🔐 Permission Matrix

| Action | Employee | Manager | HR | Board |
|--------|----------|---------|-----|-------|
| Create Request | ✅ Own | ✅ Own | ✅ Own | ✅ Own |
| Edit Pending | ✅ Own | ✅ Own | ✅ Own | ✅ Own |
| Cancel (Before Final) | ✅ Own | ✅ Own | ✅ Own | ✅ Own |
| View Own Requests | ✅ | ✅ | ✅ | ✅ |
| View Team Requests | ❌ | ✅ | ✅ | ✅ |
| View All Requests | ❌ | ❌ | ✅ | ✅ |
| Approve Team (Pending) | ❌ | ✅ | ✅ | ✅ |
| Reject Team (Pending) | ❌ | ✅ | ✅ | ✅ |
| Approve Any (Override) | ❌ | ❌ | ✅ | ✅ |
| Reject Any (Override) | ❌ | ❌ | ✅ | ✅ |
| Manage Employees | ❌ | ❌ | ✅ | ❌ |
| View Dashboard (All) | ❌ | ✅ Team | ✅ All | ✅ All |
| View Leave Balance (All) | ✅ Own | ✅ Team | ✅ All | ✅ All |

## 📊 Leave Balance Flow

### Calculation Process
```
Employee Login
    │
    ├─ Get Employee.DateOfEmployment
    ├─ Get Employee.EmployeeLevelId
    │
    ├─ Calculate Months of Service
    │   MonthsOfService = (CurrentDate - DateOfEmployment).Months
    │
    ├─ Check Probation Period
    │   IF MonthsOfService < 6
    │       → TotalAccruedLeave = 0
    │       → IsInProbation = true
    │   ELSE
    │       → Calculate Accrual
    │
    ├─ Get Annual Leave Entitlement
    │   IF EmployeeLevel = A → 15 days/year
    │   IF EmployeeLevel = B → 24 days/year
    │
    ├─ Calculate Monthly Accrual
    │   MonthlyAccrual = AnnualLeave / 12
    │
    ├─ Calculate Total Accrued Leave
    │   AccruedMonths = MonthsOfService - 6 (exclude probation)
    │   TotalAccruedLeave = MonthlyAccrual × AccruedMonths
    │
    ├─ Get Approved Transactions
    │   Query: WHERE EmployeeId = X AND StatusID = 3 (Approved by HR)
    │
    ├─ Calculate Leave Used
    │   FOR EACH Transaction:
    │       Days = Unit × Sign × (EndDate - StartDate).Days
    │       LeaveUsed += |Days|
    │
    ├─ Calculate Balance
    │   LeaveBalance = TotalAccruedLeave - LeaveUsed
    │
    └─ Return Leave Balance Report
        - Employee Info
        - Months of Service
        - Annual Entitlement
        - Monthly Accrual
        - Total Accrued
        - Leave Used
        - Current Balance
        - Probation Status
```

### Example Calculation

**Employee**: Ahmed (Level A, 15 days/year)  
**Date of Employment**: 2024-01-01  
**Current Date**: 2026-02-02  
**Months of Service**: 25 months

```
Step 1: Check Probation
    25 months > 6 months → Not in probation

Step 2: Calculate Monthly Accrual
    MonthlyAccrual = 15 / 12 = 1.25 days/month

Step 3: Calculate Total Accrued
    AccruedMonths = 25 - 6 = 19 months
    TotalAccrued = 1.25 × 19 = 23.75 days

Step 4: Calculate Leave Used
    Transaction 1: Annual Leave (5 days)
        Unit = 1, Sign = -1, Days = 5
        Used = 1 × -1 × 5 = -5 days → |5| = 5 days
    
    Transaction 2: Half Day
        Unit = 0.5, Sign = -1, Days = 1
        Used = 0.5 × -1 × 1 = -0.5 days → |0.5| = 0.5 days
    
    Total Used = 5 + 0.5 = 5.5 days

Step 5: Calculate Balance
    Balance = 23.75 - 5.5 = 18.25 days
```

## 🎯 Dashboard Chart Examples

### Example 1: Department Grouping
```
Filter: All Statuses, All Dates, Group By Department

Bar Chart:
    Quality:   ████████████ 12 requests
    IT:        ██████████ 10 requests
    Finance:   ████████ 8 requests
    HR:        ██████ 6 requests
    Marketing: ████ 4 requests

Pie Chart:
    Quality:   30% (12/40)
    IT:        25% (10/40)
    Finance:   20% (8/40)
    HR:        15% (6/40)
    Marketing: 10% (4/40)
```

### Example 2: Employee Grouping
```
Filter: Department = Quality, Group By Employee

Bar Chart:
    Ahmed (1990027):  ████████ 8 requests
    Hassan (1990055): ██████ 6 requests
    Talba (1010010):  ████ 4 requests

Pie Chart:
    Ahmed:  44.4% (8/18)
    Hassan: 33.3% (6/18)
    Talba:  22.2% (4/18)
```

## 🔄 HR Override Scenario

### Scenario: Manager Rejected, HR Overrides
```
1. Employee submits request
   Status: Pending (1)

2. Manager rejects request
   Status: Rejected by Manager (4) - FINAL

3. HR reviews and decides to override
   ❌ Cannot change - Status is FINAL
   
   Solution: HR must intervene BEFORE Manager rejects
   OR: System allows HR to "reopen" final statuses (future enhancement)
```

### Correct Override Flow
```
1. Employee submits request
   Status: Pending (1)

2. Manager about to reject, but HR intervenes
   HR Action: Approve (Override)
   Status: Approved by HR (3) - FINAL
   
   Result: HR decision overrides Manager's intended rejection
```

## 📝 Audit Trail

Every transaction maintains:
- **CreationDate**: When request was created
- **ResponseDate**: When decision was made
- **ResponseMessage**: Reason for approval/rejection
- **StatusID**: Current status
- **EmployeeId**: Who created the request

Future enhancement: Full audit log table tracking all status changes.

---

**Document Version**: 1.0.0  
**Last Updated**: 2026-02-02
