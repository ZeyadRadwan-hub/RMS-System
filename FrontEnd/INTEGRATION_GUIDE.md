# 🔗 Frontend-Backend Integration Guide

## Overview

This guide explains how the RMS Frontend connects to the Backend API.

---

## 🌐 API Configuration

### Environment Variables

The frontend uses Vite's environment variable system:

**File**: `.env`
```
VITE_API_BASE_URL=https://localhost:5001/api
```

**Usage in code**:
```javascript
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:5001/api';
```

### Change API URL

To connect to a different backend:

1. Edit `FrontEnd/.env`
2. Update `VITE_API_BASE_URL`
3. Restart dev server

**Examples**:
```bash
# Local development
VITE_API_BASE_URL=https://localhost:5001/api

# Production
VITE_API_BASE_URL=https://api.yourcompany.com/api

# Staging
VITE_API_BASE_URL=https://staging-api.yourcompany.com/api
```

---

## 🔐 Authentication Flow

### 1. Login Process

```javascript
// User enters code and password
POST /api/auth/login
{
  "code": "1980009",
  "password": "<provisioned-password>"
}

// Backend responds with user data
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

// Frontend saves to localStorage
localStorage.setItem('user', JSON.stringify(userData));
```

### 2. Authenticated Requests

Every API request automatically includes:

```javascript
headers: {
  'X-Employee-Id': user.id,
  'X-Employee-Role': user.role
}
```

This is handled by the Axios interceptor in `src/services/api.js`.

### 3. Logout

```javascript
// Clear localStorage
localStorage.removeItem('user');

// Redirect to login
window.location.href = '/login';
```

---

## 📡 API Endpoints Used

### Authentication
- `POST /api/auth/login` - User login

### Transactions (Leave Requests)
- `POST /api/transactions` - Create request
- `PUT /api/transactions/{id}` - Update request
- `POST /api/transactions/{id}/cancel` - Cancel request
- `POST /api/transactions/{id}/approve` - Approve request
- `POST /api/transactions/{id}/reject` - Reject request
- `GET /api/transactions/my-requests` - Get my requests
- `GET /api/transactions/my-team-requests` - Get team requests
- `GET /api/transactions/all` - Get all requests
- `POST /api/transactions/filter` - Filter requests

### Dashboard
- `POST /api/dashboard/stats` - Get statistics
- `POST /api/dashboard/charts` - Get chart data

### Leave Balance
- `GET /api/leavebalance/my-balance` - Get my balance
- `GET /api/leavebalance/team-balances` - Get team balances
- `GET /api/leavebalance/all` - Get all balances
- `GET /api/leavebalance/{employeeId}` - Get specific employee balance

### Employees (HR Only)
- `GET /api/employees` - Get all employees
- `GET /api/employees/{id}` - Get employee by ID
- `POST /api/employees` - Create employee
- `PUT /api/employees/{id}` - Update employee
- `DELETE /api/employees/{id}` - Delete employee

---

## 🔧 CORS Configuration

### Backend Requirements

The backend must allow CORS from the frontend origin:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5174", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

app.UseCors("AllowFrontend");
```

### Production CORS

For production, update to your actual domain:

```csharp
policy.WithOrigins("https://yourdomain.com")
```

---

## 🛡️ Error Handling

### Axios Interceptor

The frontend automatically handles common errors:

```javascript
// 401 Unauthorized - Auto logout
if (error.response?.status === 401) {
  localStorage.removeItem('user');
  window.location.href = '/login';
}

// Other errors - Return to component
return Promise.reject(error);
```

### Component Error Handling

```javascript
try {
  const data = await transactionService.getMyRequests();
  setRequests(data);
} catch (error) {
  console.error('Error:', error);
  // Show error message to user
}
```

---

## 🔄 Data Flow Example

### Creating a Leave Request

1. **User Action**: Clicks "New Request" button
2. **Frontend**: Opens modal with form
3. **User**: Fills form and submits
4. **Frontend**: Calls API
   ```javascript
   const data = {
     transactionTypesID: 2,
     startDate: "2026-03-01",
     endDate: "2026-03-05",
     substituteEmployeeId: 3,
     leaveRationale: "Vacation"
   };
   
   const result = await transactionService.create(data);
   ```
5. **Backend**: Validates and creates request
6. **Backend**: Returns created request with ID
7. **Frontend**: Updates UI, closes modal
8. **Frontend**: Refreshes request list

---

## 🧪 Testing Integration

### 1. Start Backend

```bash
cd RMS-BACKEND
dotnet run
```

Backend should be running on `https://localhost:5001`

### 2. Start Frontend

```bash
cd FrontEnd
npm run dev
```

Frontend will be on `http://localhost:5174`

### 3. Test Login

1. Open `http://localhost:5174`
2. Enter code: `1980009`
3. Enter the password from the protected provisioning file (never commit it)
4. Click "Sign In"
5. Should redirect to Dashboard

### 4. Test API Calls

Open browser DevTools (F12) → Network tab:
- See all API requests
- Verify headers include `X-Employee-Id` and `X-Employee-Role`
- Check response data

---

## 🐛 Troubleshooting

### "Network Error" or "Failed to fetch"

**Cause**: Backend not running or CORS issue

**Solution**:
1. Verify backend is running: `https://localhost:5001/api`
2. Check CORS configuration in backend
3. Verify `.env` has correct API URL

### "401 Unauthorized" on every request

**Cause**: Headers not being sent

**Solution**:
1. Check localStorage has user data
2. Verify Axios interceptor is working
3. Clear localStorage and login again

### "404 Not Found" on API calls

**Cause**: Incorrect API URL or endpoint

**Solution**:
1. Check `.env` file
2. Verify endpoint exists in backend
3. Check API_DOCUMENTATION.md for correct endpoints

### HTTPS Certificate Errors

**Cause**: Self-signed certificate in development

**Solution**:
1. Accept certificate in browser
2. Or configure backend to use HTTP in development
3. Update `.env` to use `http://` instead of `https://`

---

## 📊 Data Mapping

### User Object (Frontend)

```javascript
{
  id: 1,
  code: "1980009",
  name: "ايمن محمد الشناوى حسن",
  departmentID: 10,
  departmentName: "HR",
  role: "HR",              // Employee, Manager, HR, Board
  isManager: true,
  managerId: null
}
```

### Transaction Object

```javascript
{
  id: 1,
  transactionTypesID: 2,
  transactionTypeName: "Annual Leave",
  startDate: "2026-03-01",
  endDate: "2026-03-05",
  calculatedDays: 5,
  statusID: 1,
  statusName: "Pending",
  employeeId: 2,
  employeeName: "احمد وجدى محمد عثمان",
  canEdit: true,
  canCancel: true,
  leaveRationale: "Vacation",
  responseMessage: "",
  responseDate: null
}
```

### Leave Balance Object

```javascript
{
  employeeId: 2,
  employeeCode: "1990027",
  employeeName: "احمد وجدى محمد عثمان",
  dateOfEmployment: "1999-02-09",
  monthsOfService: 324,
  employeeLevel: "A",
  annualLeaveEntitlement: 15,
  monthlyAccrual: 1.25,
  totalAccruedLeave: 397.5,
  leaveUsed: 45.5,
  leaveBalance: 352.0,
  carryoverFromPreviousYear: 0,
  isInProbation: false,
  calculationDate: "2026-02-02"
}
```

---

## 🚀 Production Deployment

### 1. Build Frontend

```bash
npm run build
```

Output in `/dist` folder

### 2. Update Environment

Create `.env.production`:
```
VITE_API_BASE_URL=https://api.yourcompany.com/api
```

### 3. Deploy

Options:
- **Static hosting**: Netlify, Vercel, GitHub Pages
- **Web server**: Nginx, Apache
- **Cloud**: AWS S3 + CloudFront, Azure Static Web Apps

### 4. Configure Backend

Update CORS to allow production domain:
```csharp
policy.WithOrigins("https://yourcompany.com")
```

---

## 📝 Summary

The frontend is fully integrated with the backend through:
- ✅ Axios HTTP client
- ✅ Automatic authentication headers
- ✅ Error handling and redirects
- ✅ Environment-based configuration
- ✅ Complete API coverage
- ✅ Role-based access control

**Everything is connected and working! 🎉**
