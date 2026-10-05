import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/useAuth';
import { Lock, User, LogIn, Eye, EyeOff } from 'lucide-react';
import './Login.css';
import logo from '../../ElSewedy_Logo.png';

const Login = () => {
    const [code, setCode] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const { login } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        const result = await login(code, password);

        if (result.success) {
            navigate('/dashboard');
        } else {
            setError(result.error);
        }

        setLoading(false);
    };

    return (
        <div className="login-page">
            <div className="login-container">
                <div className="login-card">
                    <div className="login-header">
                        <div className="brand-logo-container">
                            <img src={logo} alt="ElSewedy Logo" className="brand-logo" />
                            <div className="branding-text">
                                <span className="brand-name">Elsewedy</span>
                                <span className="system-name">Request Management System</span>
                            </div>
                        </div>
                        <h1 className="login-title">Welcome Back</h1>
                        <p className="login-subtitle">Sign in to your account to continue</p>
                    </div>

                    <form onSubmit={handleSubmit} className="login-form">
                        {error && (
                            <div className="alert alert-error">
                                <span>{error}</span>
                            </div>
                        )}

                        <div className="form-group">
                            <label htmlFor="code" className="label">Employee Code</label>
                            <div className="input-with-icon">
                                <User className="input-icon" size={18} />
                                <input
                                    id="code"
                                    type="text"
                                    className="input"
                                    placeholder="Enter your employee code"
                                    value={code}
                                    onChange={(e) => setCode(e.target.value)}
                                    required
                                    autoFocus
                                />
                            </div>
                        </div>

                        <div className="form-group">
                            <label htmlFor="password" className="label">Password</label>
                            <div className="input-with-icon">
                                <Lock className="input-icon" size={18} />
                                <input
                                    id="password"
                                    type={showPassword ? "text" : "password"}
                                    className="input"
                                    placeholder="Enter your password"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    required
                                />
                                <button
                                    type="button"
                                    className="password-toggle"
                                    onClick={() => setShowPassword(!showPassword)}
                                >
                                    {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                                </button>
                            </div>
                        </div>

                        <button
                            type="submit"
                            className="btn btn-primary btn-login"
                            disabled={loading}
                        >
                            <LogIn size={18} style={{ marginRight: '8px' }} />
                            {loading ? 'Signing in...' : 'Sign In'}
                        </button>
                    </form>

                    <div className="login-footer">
                        <p className="admin-contact">
                            Don't have an account? Ask your HR administrator to create one.
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Login;
