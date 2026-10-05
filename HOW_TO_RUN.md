# 🚀 RMS Leave Management System - Quick Start Guide

## ✅ System Status

Your RMS Leave Management System is now **READY TO USE**!

---

## 🌐 Access URLs

### **Frontend (React + Vite)**
👉 **http://localhost:5174/**

- Login page with red gradient design
- Role-based dashboards
- ag-Grid tables for data management
- Real-time leave balance calculations

### **Backend API (ASP.NET Core)**
👉 **https://localhost:5001/**

- Swagger UI for API testing
- RESTful endpoints
- SQL Server database integration
- Complete business logic implementation

---

## 🔑 Test Credentials

### HR Manager (Full Access)
- **Code**: `1980009`
- **Password**: `<provisioned-password>` (read from the protected provisioning file)
- **Access**: Dashboard, My Requests, Staff Requests, Employee List, Profile

### Employee (Quality Department)
- **Code**: `1990027`
- **Password**: `<provisioned-password>` (read from the protected provisioning file)
- **Access**: Profile, My Requests, Leave Balance

### Employee (Quality Department)
- **Code**: `1990055`
- **Password**: `<provisioned-password>` (read from the protected provisioning file)
- **Access**: Profile, My Requests, Leave Balance

---

## 📋 How to Run (If Servers Are Not Running)

### **1. Start Backend API**
```bash
cd c:\Users\hp\source\repos\RMS-BACKEND\RMS-BACKEND
dotnet run
```

**Expected Output:**
```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

### **2. Start Frontend (In a NEW Terminal)**
```bash
cd c:\Users\hp\source\repos\RMS-BACKEND\FrontEnd
npm run dev
```

**Expected Output:**
```
VITE v7.3.1  ready in XXXXms
➜  Local:   http://localhost:5174/
```

---

## 🎯 Testing the System

### **Step 1: Login**
1. Open **http://localhost:5174/** in your browser
2. Enter employee code: `1980009`
3. Enter the password from the protected provisioning file (never commit it)
4. Click **Sign In**

### **Step 2: Explore Dashboard**
- View statistics (Total, Pending, Approved, Rejected requests)
- See role-based navigation in the left sidebar
- Red gradient theme throughout

### **Step 3: Test Leave Requests**
1. Click **My Requests** in sidebar
2. Click **Submit New Request** button
3. Fill in leave details
4. Submit and see it in the table

### **Step 4: Test Leave Balance**
1. Click **Leave Balance** in sidebar
2. View your accrued leave
3. See dynamic calculations based on:
   - Months of service
   - Probation period (first 6 months)
   - Annual entitlement (Level A = 15 days, Level B = 24 days)
   - Monthly accrual rate

### **Step 5: HR Features (Login as HR)**
1. Logout and login as `1980009`
2. Navigate to **Employee List**
3. View all employees in ag-Grid table
4. Test search functionality
5. Navigate to **Staff Requests**
6. Approve/Reject requests with override authority

---

## 🔧 Troubleshooting

### **Frontend Not Loading?**
```bash
cd c:\Users\hp\source\repos\RMS-BACKEND\FrontEnd
npm install --legacy-peer-deps
npm run dev
```

### **Backend API Error?**
```bash
cd c:\Users\hp\source\repos\RMS-BACKEND\RMS-BACKEND
dotnet clean
dotnet build
dotnet run
```

### **Database Connection Issues?**
1. Open `RMS-BACKEND\appsettings.json`
2. Verify connection string points to your SQL Server instance
3. Ensure database `RMS` exists with all tables populated

### **CORS Errors?**
The backend is configured to allow all origins in development. If you still see CORS errors:
1. Check that backend is running on `https://localhost:5001`
2. Verify `apiClient.js` has correct `API_BASE_URL`

---

## 📊 Features Implemented

### ✅ **Authentication & Authorization**
- Login with employee code and password
- Role determination (Board, HR, Manager, Employee)
- Protected routes based on roles

### ✅ **Request Management**
- Create, edit, cancel leave requests
- State machine enforcement (Pending → Pending HR → Approved/Rejected)
- Manager approval workflow
- HR override authority

### ✅ **Leave Balance Calculation**
- Dynamic calculation: `Unit × Sign × (EndDate - StartDate).Days`
- Monthly accrual: `Annual Leave / 12`
- Probation period handling (6 months)
- Real-time balance updates

### ✅ **Dashboard & Analytics**
- Role-based statistics
- Department/Employee grouping
- Chart placeholders for ag-Grid Charts
- Filter capabilities

### ✅ **Employee Management (HR)**
- View all employees
- Add/Edit/Delete employees
- Search and filter
- ag-Grid integration

### ✅ **UI/UX Excellence**
- Red gradient theme (#C4161C to #E53935)
- White card-based layouts
- Soft shadows and rounded corners
- Responsive sidebar navigation
- ag-Grid tables with theme matching
- Clean, modern typography (Inter/Poppins/Roboto)

---

## 🎨 Design Highlights

- **Primary Color**: Red gradient
- **Background**: Light gray (#F8F9FB)
- **Cards**: White with 12px border radius
- **Shadows**: Soft (0 4px 20px rgba(0,0,0,0.05))
- **Typography**: Inter, Poppins, Roboto
- **Tables**: ag-Grid with zebra rows
- **Buttons**: Red gradient primary, white secondary

---

## 📁 Project Structure

```
RMS-BACKEND/
├── RMS-BACKEND/              # Backend API (.NET)
│   ├── Controllers/          # API endpoints
│   ├── Services/             # Business logic
│   ├── Repositories/         # Data access
│   ├── Models/               # Entity models
│   └── DTOs/                 # Data transfer objects
│
├── FrontEnd/                 # Frontend (React + Vite)
│   ├── src/
│   │   ├── components/       # Reusable UI components
│   │   ├── pages/            # Route pages
│   │   ├── services/         # API communication
│   │   ├── context/          # Global state (Auth)
│   │   └── theme/            # Design tokens
│   └── package.json
│
└── Documentation/
    ├── README.md
    ├── API_DOCUMENTATION.md
    ├── FLOW_DIAGRAM.md
    └── QUICK_START.md
```

---

## 🎉 Success Checklist

- [x] Backend API running on https://localhost:5001
- [x] Frontend running on http://localhost:5174
- [x] Database connected and populated
- [x] Login working with test credentials
- [x] Dashboard displaying statistics
- [x] Leave requests CRUD operations
- [x] Leave balance calculations
- [x] Role-based navigation
- [x] ag-Grid tables functional
- [x] Red gradient theme applied
- [x] Responsive design working

---

## 🚀 Next Steps

1. **Test All Workflows**: Login as different roles and test all features
2. **Populate More Data**: Add more employees and transactions for testing
3. **Implement Charts**: Add ag-Grid Charts for bar and pie charts
4. **Add Validations**: Enhance form validations on frontend
5. **Review session operations**: use the approved password-reset/onboarding workflow
6. **Operationalize security**: configure rate limiting, monitoring, and HTTPS
7. **Deploy to Production**: Configure for production environment

---

**Your RMS Leave Management System is LIVE! 🎊**

Access it now at: **http://localhost:5174/**

---

**Version**: 1.0.0  
**Last Updated**: 2026-02-02
