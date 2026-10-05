import React, { lazy, Suspense } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { NotificationProvider } from './context/NotificationContext';
import ProtectedRoute from './components/ProtectedRoute';
import BlockBoardRoute from './components/BlockBoardRoute';
import Sidebar from './components/Sidebar';
import Loading from './components/Loading';
import Login from './pages/Login';
import './App.css';

const Dashboard = lazy(() => import('./pages/Dashboard'));
const MyRequests = lazy(() => import('./pages/MyRequests'));
const TeamRequests = lazy(() => import('./pages/TeamRequests'));
const AllRequests = lazy(() => import('./pages/AllRequests'));
const Employees = lazy(() => import('./pages/Employees'));
const LeaveBalance = lazy(() => import('./pages/LeaveBalance'));
const HRRequests = lazy(() => import('./pages/HRRequests'));
const History = lazy(() => import('./pages/History'));
const Profile = lazy(() => import('./pages/Profile'));

function App() {
  return (
    <AuthProvider>
      <NotificationProvider>
        <Router>
          <Routes>
            {/* Public Route */}
            <Route path="/login" element={<Login />} />

            {/* Protected Routes */}
            <Route
              path="/*"
              element={
                <ProtectedRoute>
                  <AppLayout />
                </ProtectedRoute>
              }
            />
          </Routes>
        </Router>
      </NotificationProvider>
    </AuthProvider>
  );
}

// Layout component with sidebar
function AppLayout() {
  return (
    <div className="app-layout">
      <Sidebar />
      <main className="main-content">
        <Suspense fallback={<Loading message="Loading page..." />}>
        <Routes>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/my-requests" element={<BlockBoardRoute><MyRequests /></BlockBoardRoute>} />
          <Route path="/leave-balance" element={
            <BlockBoardRoute>
              <LeaveBalance />
            </BlockBoardRoute>
          } />
          <Route path="/profile" element={<Profile />} />

          {/* Manager Routes */}
          <Route
            path="/team-requests"
            element={
              <ProtectedRoute requiredRole="Manager">
                <TeamRequests />
              </ProtectedRoute>
            }
          />

          {/* HR Routes */}
          <Route
            path="/all-requests"
            element={
              <ProtectedRoute requiredRole="HR">
                <AllRequests />
              </ProtectedRoute>
            }
          />
          <Route
            path="/employees"
            element={
              <ProtectedRoute requiredRole="HR">
                <Employees />
              </ProtectedRoute>
            }
          />

          {/* Board Routes */}
          <Route
            path="/history"
            element={
              <ProtectedRoute requiredRole="Board">
                <History />
              </ProtectedRoute>
            }
          />
          <Route
            path="/hr-requests"
            element={
              <ProtectedRoute requiredRole="Board">
                <HRRequests />
              </ProtectedRoute>
            }
          />

          {/* 404 */}
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
        </Suspense>
      </main>
    </div>
  );
}

export default App;
