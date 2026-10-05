# 🚀 Quick Start Guide - RMS Frontend

## Prerequisites
- Node.js 18+ installed
- Backend API running on `https://localhost:5001`

## Installation & Setup

### 1. Install Dependencies
```bash
cd FrontEnd
npm install
```

### 2. Configure Environment
The `.env` file is already configured for local development:
```
VITE_API_BASE_URL=https://localhost:5001/api
```

If your backend runs on a different URL, update this file.

### 3. Start Development Server
```bash
npm run dev
```

The app will open at `http://localhost:5173`

## 🔐 Login Credentials

Use these credentials from the backend database:

### HR Manager
- **Code**: `1980009`
- **Password**: `<provisioned-password>` (read from the protected provisioning file)
- **Role**: HR (Full access)

### Regular Employee
- **Code**: `1990027`
- **Password**: `<provisioned-password>` (read from the protected provisioning file)
- **Role**: Employee

## 📱 Available Pages

After logging in, you can access:

### All Roles
- **Dashboard** - Statistics and charts
- **My Requests** - Create and manage leave requests
- **Leave Balance** - View your leave balance
- **Profile** - Your account information

### Manager Role (Additional)
- **Team Requests** - Approve/reject team member requests
- **Team Leave Balances** - View team balances

### HR Role (Additional)
- **All Requests** - View and manage all company requests
- **Employees** - Manage employee records
- **All Leave Balances** - Company-wide balance reports

### Board Role
- **History** - All historical requests
- **HR Requests** - HR-specific requests

## 🎨 Features to Explore

1. **Dashboard Charts**
   - Interactive bar and pie charts
   - Filter by date range, department, or employee
   - Real-time statistics

2. **Request Management**
   - Create new leave requests
   - Edit pending requests
   - Cancel requests before approval
   - View request history

3. **ag-Grid Tables**
   - Sort by any column
   - Filter data
   - Pagination
   - Export capabilities

4. **Leave Balance**
   - Real-time balance calculation
   - Historical balance queries
   - Team/company-wide views

## 🛠️ Development Commands

```bash
# Start dev server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run linter
npm run lint
```

## 🔧 Troubleshooting

### Backend Connection Issues
If you see connection errors:
1. Ensure backend is running on `https://localhost:5001`
2. Check `.env` file has correct API URL
3. Verify CORS is enabled in backend

### Build Errors
```bash
# Clear node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

### Port Already in Use
```bash
# Vite will automatically try the next available port
# Or specify a custom port:
npm run dev -- --port 3000
```

## 📦 Production Build

```bash
# Build optimized production bundle
npm run build

# Output will be in /dist folder
# Serve with any static file server
```

## 🎯 Next Steps

1. **Explore the Dashboard** - View statistics and charts
2. **Create a Leave Request** - Test the request workflow
3. **Check Leave Balance** - View your accrued leave
4. **Test Role-Based Access** - Login with different roles

## 💡 Tips

- Use the sidebar to navigate between pages
- Click the collapse button to minimize the sidebar
- All tables support sorting and filtering
- Status badges are color-coded for easy identification
- The app is fully responsive - try it on mobile!

## 📞 Support

For issues or questions:
- Check the main README.md
- Review API_DOCUMENTATION.md in the backend
- Verify backend is running and seeded with data

---

**Enjoy using the RMS Leave Management System! 🎉**
