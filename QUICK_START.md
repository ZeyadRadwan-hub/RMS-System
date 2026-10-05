# 🚀 Quick Start Guide - RMS Leave Management Backend

## Prerequisites Checklist
- [ ] .NET 10.0 SDK installed
- [ ] SQL Server (LocalDB or full instance) installed
- [ ] Visual Studio 2022 or VS Code
- [ ] Postman or similar API testing tool (optional)

## Step-by-Step Setup

### 1. Database Setup

The database is already created with the provided SQL script. Ensure your connection string in `appsettings.json` matches your SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\ProjectModels;Initial Catalog=RMS;Integrated Security=True;..."
  }
}
```

**Verify Database:**
- Open SQL Server Management Studio (SSMS)
- Connect to `(localdb)\ProjectModels`
- Verify database `RMS` exists
- Verify tables: Employees, EmployeeLevel, Status, TransactionTypes, Transactions

### 2. Build the Project

```bash
cd c:\Users\hp\source\repos\RMS-BACKEND\RMS-BACKEND
dotnet restore
dotnet build
```

Expected output: `Build succeeded`

### 3. Run the Application

```bash
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
```

### 4. Access Swagger UI

Open your browser and navigate to:
```
https://localhost:5001
```

You should see the Swagger UI with all API endpoints documented.

### 5. Test the API

#### Option A: Using Swagger UI

1. Click on `POST /api/auth/login`
2. Click "Try it out"
3. Enter credentials:
```json
{
  "code": "1980009",
  "password": "<provisioned-password>"
}
```
4. Click "Execute"
5. Copy the `id` and `role` from the response

#### Option B: Using Postman

Import the following request:

**Login:**
```http
POST https://localhost:5001/api/auth/login
Content-Type: application/json

{
  "code": "1980009",
  "password": "<provisioned-password>"
}
```

**Create Leave Request:**
```http
POST https://localhost:5001/api/transactions
X-Employee-Id: 2
Content-Type: application/json

{
  "transactionTypesID": 2,
  "startDate": "2026-03-01",
  "endDate": "2026-03-05",
  "substituteEmployeeId": 3,
  "leaveRationale": "Family vacation"
}
```

## 🧪 Testing Scenarios

### Scenario 1: Employee Flow
1. Login as employee (Code: 1990027)
2. Create a leave request
3. View my requests
4. Edit the pending request
5. Cancel the request

### Scenario 2: Manager Flow
1. Login as manager (Code: 1980009)
2. View team requests
3. Approve a team member's request
4. Check leave balance for team

### Scenario 3: HR Flow
1. Login as HR (Code: 1980009, DepartmentID: 10)
2. View all requests
3. Override approve any request
4. View dashboard with filters
5. Manage employees

### Scenario 4: Leave Balance Calculation
1. Login as any employee
2. GET /api/leavebalance/my-balance
3. Verify calculation:
   - Months of service
   - Probation status
   - Accrued leave
   - Used leave
   - Current balance

## 📊 Sample Test Data

### Existing Employees
```
ID: 1, Code: 1980009, Name: ايمن محمد الشناوى حسن, Role: Manager, Dept: HR (10)
ID: 2, Code: 1990027, Name: احمد وجدى محمد عثمان, Role: Employee, Dept: Quality (7)
ID: 3, Code: 1990055, Name: حسن ابراهيم عبد القادر حسن, Role: Employee, Dept: Quality (7)
```

### Transaction Types
```
1 - Casual Leave (Full day, deduction)
2 - Annual Leave (Full day, deduction)
3 - Half Day (0.5 day, deduction)
4 - Quarter Day (0.25 day, deduction)
```

### Status Flow
```
1 - Pending → Can edit, can cancel
2 - Pending HR → Cannot edit, can cancel
3 - Approved by HR → FINAL
4 - Rejected by Manager → FINAL
5 - Rejected by HR → FINAL
6 - Cancelled by Employee → FINAL
```

## 🔧 Troubleshooting

### Issue: Build Failed
**Solution:** Ensure all NuGet packages are restored
```bash
dotnet restore
dotnet clean
dotnet build
```

### Issue: Database Connection Error
**Solution:** Update connection string in `appsettings.json`
- Verify SQL Server is running
- Check instance name
- Verify database exists

### Issue: Port Already in Use
**Solution:** Change port in `Properties/launchSettings.json` or stop the conflicting process

### Issue: CORS Error
**Solution:** CORS is already configured to allow all origins in development. For production, update the CORS policy in `Program.cs`

## 📝 Next Steps

1. **Test All Endpoints**: Use Swagger UI or Postman to test each endpoint
2. **Create Test Data**: Add more employees and transactions for testing
3. **Test State Machine**: Verify all status transitions work correctly
4. **Test Permissions**: Verify role-based access control
5. **Test Leave Calculation**: Verify leave balance calculations are accurate

## 🎯 Key Features to Test

- [x] Login and role determination
- [x] Create leave request
- [x] Edit pending request
- [x] Cancel request
- [x] Manager approve/reject
- [x] HR override
- [x] Leave balance calculation
- [x] Dashboard with filters
- [x] Chart data generation
- [x] Employee management (HR)

## 📞 Common API Calls

### Get My Requests
```http
GET https://localhost:5001/api/transactions/my-requests
X-Employee-Id: 2
```

### Get Dashboard Stats
```http
POST https://localhost:5001/api/dashboard/stats
X-Employee-Id: 1
X-Employee-Role: HR
Content-Type: application/json

{
  "groupBy": "Department"
}
```

### Get Leave Balance
```http
GET https://localhost:5001/api/leavebalance/my-balance
X-Employee-Id: 2
```

## 🔐 Security Notes

**Current Implementation:**
- Database-backed bearer sessions with expiry and logout/revocation
- Passwords stored as individual ASP.NET `PasswordHasher` hashes
- Authorization enforced from the authenticated employee and role

**Production Recommendations:**
- Add a controlled password-reset/onboarding workflow
- Keep rate limiting and monitoring enabled in the deployment edge
- Run behind HTTPS and replace the local connection string per environment

## 📚 Documentation

- **README.md** - Complete system overview
- **FLOW_DIAGRAM.md** - State diagram and flow documentation
- **API_DOCUMENTATION.md** - Detailed API reference
- **Swagger UI** - Interactive API documentation (https://localhost:5001)

## ✅ Verification Checklist

After setup, verify:
- [ ] Application starts without errors
- [ ] Swagger UI is accessible
- [ ] Login endpoint works
- [ ] Can create a leave request
- [ ] Can view requests
- [ ] Leave balance calculation works
- [ ] Dashboard returns data
- [ ] All CRUD operations work

## 🎉 Success!

If all steps completed successfully, your RMS Leave Management Backend is ready to use!

---

**Need Help?**
- Check the error logs in the console
- Review the API documentation
- Verify database connection
- Ensure all prerequisites are installed

**Version**: 1.0.0  
**Last Updated**: 2026-02-02
