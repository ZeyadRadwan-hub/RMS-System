# RMS Leave Management - API Documentation

## 🌐 Base URL
```
Development: http://localhost:5190/api
Production: https://your-domain.com/api
```

## 🔐 Authentication

All protected endpoints require an expiring bearer session token returned by login. Identity and role are verified on the server:
```http
Authorization: Bearer <session-token>
```

---

## 📚 API Endpoints

### 1. Authentication

#### POST /auth/login
Login and retrieve user information with role determination.

**Request:**
```json
{
  "code": "1980009",
  "password": "<current-test-password>"
}
```

**Response:**
```json
{
  "id": 1,
  "code": "1980009",
  "name": "ايمن محمد الشناوى حسن",
  "departmentID": 10,
  "departmentName": "HR",
  "role": "HR",
  "isManager": true,
  "managerId": null,
  "token": "base64-encoded-token"
}
```

**Status Codes:**
- `200 OK`: Login successful
- `401 Unauthorized`: Invalid credentials
- `500 Internal Server Error`: Server error

---

### 2. Transactions (Leave Requests)

#### POST /transactions
Create a new leave request.

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Request:**
```json
{
  "transactionTypesID": 2,
  "startDate": "2026-03-01",
  "endDate": "2026-03-05",
  "substituteEmployeeId": 3,
  "leaveRationale": "Family vacation"
}
```

**Response:**
```json
{
  "id": 1,
  "transactionTypesID": 2,
  "transactionTypeName": "Annual Leave",
  "startDate": "2026-03-01",
  "endDate": "2026-03-05",
  "substituteEmployeeId": 3,
  "substituteEmployeeName": "حسن ابراهيم عبد القادر حسن",
  "leaveRationale": "Family vacation",
  "responseDate": null,
  "responseMessage": "",
  "statusID": 1,
  "statusName": "Pending",
  "creationDate": "2026-02-02",
  "employeeId": 2,
  "employeeName": "احمد وجدى محمد عثمان",
  "employeeCode": "1990027",
  "departmentName": "Quality",
  "calculatedDays": 5,
  "canEdit": true,
  "canCancel": true
}
```

---

#### PUT /transactions/{id}
Update a pending leave request (only while status = Pending).

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Request:**
```json
{
  "transactionTypesID": 2,
  "startDate": "2026-03-01",
  "endDate": "2026-03-07",
  "substituteEmployeeId": 3,
  "leaveRationale": "Extended family vacation"
}
```

**Response:** Same as POST /transactions

**Status Codes:**
- `200 OK`: Updated successfully
- `400 Bad Request`: Cannot edit (not pending or not owner)

---

#### POST /transactions/{id}/cancel
Cancel a leave request (before final decision).

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Response:**
```json
{
  "id": 1,
  "statusID": 6,
  "statusName": "Cancelled by Employee",
  "responseDate": "2026-02-02",
  "responseMessage": "Cancelled by employee",
  "canEdit": false,
  "canCancel": false,
  ...
}
```

---

#### POST /transactions/{id}/approve
Approve a leave request (Manager/HR/Board).

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Request:**
```json
{
  "transactionId": 1,
  "responseMessage": "Approved for the requested dates"
}
```

**Response:**
```json
{
  "id": 1,
  "statusID": 2,  // or 3 if HR/Board
  "statusName": "Pending HR",  // or "Approved by HR"
  "responseDate": "2026-02-02",
  "responseMessage": "Approved for the requested dates",
  ...
}
```

**Status Codes:**
- `200 OK`: Approved successfully
- `400 Bad Request`: No permission to approve

---

#### POST /transactions/{id}/reject
Reject a leave request (Manager/HR/Board).

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Request:**
```json
{
  "transactionId": 1,
  "responseMessage": "Insufficient coverage during requested period"
}
```

**Response:**
```json
{
  "id": 1,
  "statusID": 4,  // or 5 if HR/Board
  "statusName": "Rejected by Manager",  // or "Rejected by HR"
  "responseDate": "2026-02-02",
  "responseMessage": "Insufficient coverage during requested period",
  "canEdit": false,
  "canCancel": false,
  ...
}
```

---

#### GET /transactions/{id}
Get a specific transaction by ID.

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Response:** Same as POST /transactions

---

#### GET /transactions/my-requests?page=1&pageSize=100
Get one bounded page of requests for the logged-in employee. `pageSize` is clamped to 200. The response shape is `{ items, page, pageSize, totalCount, hasNext }`.

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Response:**
```json
{
  "items": [
  {
    "id": 1,
    "transactionTypeName": "Annual Leave",
    "startDate": "2026-03-01",
    "endDate": "2026-03-05",
    "statusName": "Pending",
    "calculatedDays": 5,
    "canEdit": true,
    "canCancel": true,
    ...
  },
    ...
  ],
  "page": 1,
  "pageSize": 100,
  "totalCount": 3,
  "hasNext": false
}
```

---

#### GET /transactions/my-team-requests
Get all requests for the manager's team.

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Query:** `page` and `pageSize` (maximum 200). The response is a bounded page object with `items`, `totalCount`, and `hasNext`.

---

#### GET /transactions/all
Get all requests (HR/Board only).

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Query:** `page` and `pageSize` (maximum 200). The response is a bounded page object with `items`, `totalCount`, and `hasNext`.

---

#### POST /transactions/filter
Get filtered requests with advanced filtering.

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Request:**
```json
{
  "statusID": 1,
  "departmentID": 7,
  "employeeId": null,
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "groupBy": "Department"
}
```

**Query:** `page` and `pageSize` (maximum 200). The response is a bounded page object with `items`, `totalCount`, and `hasNext`.

---

### 3. Dashboard

#### POST /dashboard/stats
Get dashboard statistics with filters.

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Request:**
```json
{
  "statusID": null,
  "departmentID": null,
  "employeeId": null,
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "groupBy": "Department"
}
```

**Response:**
```json
{
  "totalRequests": 40,
  "pendingRequests": 12,
  "approvedRequests": 20,
  "rejectedRequests": 5,
  "cancelledRequests": 3,
  "departmentStats": [
    {
      "departmentID": 7,
      "departmentName": "Quality",
      "requestCount": 12
    },
    {
      "departmentID": 8,
      "departmentName": "Marketing",
      "requestCount": 10
    }
  ],
  "employeeStats": [],
  "statusStats": [
    {
      "statusID": 1,
      "statusName": "Pending",
      "requestCount": 12
    },
    {
      "statusID": 3,
      "statusName": "Approved by HR",
      "requestCount": 20
    }
  ]
}
```

---

#### POST /dashboard/charts
Get chart data (bar and pie charts).

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Request:**
```json
{
  "statusID": null,
  "departmentID": null,
  "employeeId": null,
  "startDate": null,
  "endDate": null,
  "groupBy": "Department"
}
```

**Response:**
```json
{
  "barChartData": [
    {
      "label": "Quality",
      "value": 12
    },
    {
      "label": "Marketing",
      "value": 10
    },
    {
      "label": "Finance",
      "value": 8
    }
  ],
  "pieChartData": [
    {
      "label": "Quality",
      "value": 12,
      "percentage": 30.0
    },
    {
      "label": "Marketing",
      "value": 10,
      "percentage": 25.0
    },
    {
      "label": "Finance",
      "value": 8,
      "percentage": 20.0
    }
  ]
}
```

---

### 4. Leave Balance

#### GET /leavebalance/{employeeId}
Get leave balance for a specific employee.

**Query Parameters:**
- `asOfDate` (optional): Calculate balance as of this date (default: today)

**Example:**
```http
GET /leavebalance/2?asOfDate=2026-02-02
```

**Response:**
```json
{
  "employeeId": 2,
  "employeeCode": "1990027",
  "employeeName": "احمد وجدى محمد عثمان",
  "dateOfEmployment": "1999-02-09",
  "monthsOfService": 324,
  "employeeLevel": "A",
  "annualLeaveEntitlement": 15,
  "monthlyAccrual": 1.25,
  "totalAccruedLeave": 397.5,
  "leaveUsed": 45.5,
  "leaveBalance": 352.0,
  "carryoverFromPreviousYear": 0,
  "isInProbation": false,
  "calculationDate": "2026-02-02"
}
```

---

#### GET /leavebalance/my-balance
Get leave balance for the logged-in employee.

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Query Parameters:**
- `asOfDate` (optional)

**Response:** Same as GET /leavebalance/{employeeId}

---

#### GET /leavebalance/team-balances
Get leave balances for the manager's team.

**Headers:**
```http
Authorization: Bearer <session-token>
```

**Query Parameters:**
- `asOfDate` (optional)

**Response:** Array of leave balance objects

---

#### GET /leavebalance/department/{departmentId}
Get leave balances for a department (HR/Board).

**Query Parameters:**
- `asOfDate` (optional)

**Example:**
```http
GET /leavebalance/department/7?asOfDate=2026-02-02
```

**Response:** Array of leave balance objects for department

---

#### GET /leavebalance/all
Get all leave balances (HR/Board only).

**Query Parameters:**
- `asOfDate` (optional)

**Response:** Array of all employee leave balances

---

### 5. Employees (HR Only)

#### GET /employees
Get all employees.

**Response:**
```json
[
  {
    "id": 1,
    "code": "1980009",
    "name": "ايمن محمد الشناوى حسن",
    "dateOfEmployment": "1998-07-01",
    "employeeRole": "Manager",
    "employeeLevel": "B",
    "managerId": null,
    "managerName": null,
    "departmentID": 10,
    "departmentName": "HR",
    "isActive": true
  },
  ...
]
```

---

#### GET /employees/{id}
Get employee by ID.

**Response:** Same as GET /employees (single object)

---

#### POST /employees
Create a new employee.

**Request:**
```json
{
  "code": "2026001",
  "name": "New Employee",
  "password": "<current-test-password>",
  "dateOfEmployment": "2026-02-01",
  "employeeRole": 0,
  "employeeLevelId": 1,
  "managerId": 1,
  "departmentID": 7
}
```

**Response:** Created employee object

**Status Codes:**
- `200 OK`: Created successfully
- `400 Bad Request`: Code already exists

---

#### PUT /employees/{id}
Update an employee.

**Request:**
```json
{
  "code": "2026001",
  "name": "Updated Employee Name",
  "dateOfEmployment": "2026-02-01",
  "employeeRole": 1,
  "employeeLevelId": 2,
  "managerId": 1,
  "departmentID": 7
}
```

**Response:** Updated employee object

---

#### DELETE /employees/{id}
Soft delete an employee.

**Response:**
```json
{
  "message": "Employee deleted successfully"
}
```

---

## 📊 Data Models

### Transaction Status IDs
```
1 - Pending
2 - Pending HR
3 - Approved by HR
4 - Rejected by Manager
5 - Rejected by HR
6 - Cancelled by Employee
```

### Department IDs
```
7  - Quality
8  - Marketing
9  - Finance
10 - HR
11 - Board
```

### Transaction Type IDs
```
1  - Casual Leave (Unit: 1, Sign: -1)
2  - Annual Leave (Unit: 1, Sign: -1)
3  - Half Day (Unit: 0.5, Sign: -1)
4  - Quarter Day (Unit: 0.25, Sign: -1)
5  - Additional Regular Leave (Unit: 1, Sign: +1)
6  - Additional Casual Leave (Unit: 1, Sign: +1)
50 - Sick Leave (Unit: 1, Sign: 0)
```

### Employee Level IDs
```
1 - Level A (15 days/year)
2 - Level B (24 days/year)
```

### Employee Roles
```
0 - Employee
1 - Manager
```

---

## 🔒 Authorization Rules

### Endpoint Access Matrix

| Endpoint | Employee | Manager | HR | Board |
|----------|----------|---------|-----|-------|
| POST /transactions | ✅ | ✅ | ✅ | ✅ |
| PUT /transactions/{id} | ✅ Own | ✅ Own | ✅ Own | ✅ Own |
| POST /transactions/{id}/cancel | ✅ Own | ✅ Own | ✅ Own | ✅ Own |
| POST /transactions/{id}/approve | ❌ | ✅ Team | ✅ All | ✅ All |
| POST /transactions/{id}/reject | ❌ | ✅ Team | ✅ All | ✅ All |
| GET /transactions/my-requests | ✅ | ✅ | ✅ | ✅ |
| GET /transactions/my-team-requests | ❌ | ✅ | ✅ | ✅ |
| GET /transactions/all | ❌ | ❌ | ✅ | ✅ |
| POST /dashboard/stats | ✅ Own | ✅ Team | ✅ All | ✅ All |
| POST /dashboard/charts | ✅ Own | ✅ Team | ✅ All | ✅ All |
| GET /leavebalance/my-balance | ✅ | ✅ | ✅ | ✅ |
| GET /leavebalance/team-balances | ❌ | ✅ | ✅ | ✅ |
| GET /leavebalance/all | ❌ | ❌ | ✅ | ✅ |
| GET /employees | ❌ | ❌ | ✅ | ❌ |
| POST /employees | ❌ | ❌ | ✅ | ❌ |
| PUT /employees/{id} | ❌ | ❌ | ✅ | ❌ |
| DELETE /employees/{id} | ❌ | ❌ | ✅ | ❌ |

---

## 🧪 Testing Examples

### Postman Collection Structure
```
RMS Leave Management API
├── Authentication
│   └── Login
├── Transactions
│   ├── Create Request
│   ├── Update Request
│   ├── Cancel Request
│   ├── Approve Request
│   ├── Reject Request
│   ├── Get My Requests
│   ├── Get Team Requests
│   └── Get All Requests
├── Dashboard
│   ├── Get Stats
│   └── Get Charts
├── Leave Balance
│   ├── Get My Balance
│   ├── Get Team Balances
│   └── Get All Balances
└── Employees
    ├── Get All Employees
    ├── Create Employee
    ├── Update Employee
    └── Delete Employee
```

### Sample Test Scenarios

#### Scenario 1: Employee Creates and Cancels Request
```http
# 1. Login
POST /api/auth/login
{
  "code": "1990027",
  "password": "<current-test-password>"
}

# 2. Create request
POST /api/transactions
Authorization: Bearer <session-token>
{
  "transactionTypesID": 2,
  "startDate": "2026-03-01",
  "endDate": "2026-03-05",
  "substituteEmployeeId": 3,
  "leaveRationale": "Vacation"
}

# 3. Cancel request
POST /api/transactions/1/cancel
Authorization: Bearer <session-token>
```

#### Scenario 2: Manager Approves Team Request
```http
# 1. Login as manager
POST /api/auth/login
{
  "code": "1980009",
  "password": "<current-test-password>"
}

# 2. Get team requests
GET /api/transactions/my-team-requests
Authorization: Bearer <session-token>

# 3. Approve request
POST /api/transactions/1/approve
Authorization: Bearer <session-token>
{
  "transactionId": 1,
  "responseMessage": "Approved"
}
```

#### Scenario 3: HR Override
```http
# 1. Login as HR
POST /api/auth/login
{
  "code": "1980009",
  "password": "<current-test-password>"
}

# 2. Approve any request (override)
POST /api/transactions/1/approve
Authorization: Bearer <session-token>
{
  "transactionId": 1,
  "responseMessage": "HR Override - Approved"
}
```

---

## 📝 Error Responses

### Standard Error Format
```json
{
  "message": "Error description"
}
```

### Common Error Codes
- `400 Bad Request`: Invalid input or business rule violation
- `401 Unauthorized`: Invalid credentials
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

### Example Error Responses

**Cannot Edit (Not Pending):**
```json
{
  "message": "Cannot edit this request. Only pending requests can be edited."
}
```

**No Permission:**
```json
{
  "message": "You do not have permission to approve this request."
}
```

**Employee Not Found:**
```json
{
  "message": "Employee with ID 999 not found"
}
```

---

## 🔄 Versioning

Current API Version: **v1**

Future versions will be accessible via:
```
/api/v2/...
```

---

**Document Version**: 1.0.0  
**Last Updated**: 2026-02-02
