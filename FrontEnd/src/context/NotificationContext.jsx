import React, { useState, useCallback } from 'react';
import { AlertCircle, CheckCircle, Info, XCircle, HelpCircle } from 'lucide-react';
import './Notification.css';
import NotificationContext from './notification-context';

export const NotificationProvider = ({ children }) => {
    const [dialog, setDialog] = useState(null);

    const showAlert = useCallback((message, type = 'error') => {
        return new Promise((resolve) => {
            setDialog({
                type: 'alert',
                message,
                status: type, // 'error', 'success', 'info', 'warning'
                resolve
            });
        });
    }, []);

    const showConfirm = useCallback((message, title = 'Confirm Action') => {
        return new Promise((resolve) => {
            setDialog({
                type: 'confirm',
                title,
                message,
                resolve
            });
        });
    }, []);

    const showPrompt = useCallback((message, title = 'Input Required', defaultValue = '') => {
        return new Promise((resolve) => {
            setDialog({
                type: 'prompt',
                title,
                message,
                defaultValue,
                resolve
            });
        });
    }, []);

    const handleConfirm = (value) => {
        const resolve = dialog.resolve;
        setDialog(null);
        resolve(value);
    };

    return (
        <NotificationContext.Provider value={{ showAlert, showConfirm, showPrompt }}>
            {children}
            {dialog && (
                <div className="notification-overlay">
                    <div className={`notification-modal ${dialog.type}`}>
                        {dialog.type === 'alert' && (
                            <div className="notification-content">
                                <div className={`notification-icon ${dialog.status}`}>
                                    {dialog.status === 'success' && <CheckCircle size={48} />}
                                    {dialog.status === 'error' && <XCircle size={48} />}
                                    {dialog.status === 'warning' && <AlertCircle size={48} />}
                                    {(dialog.status === 'info' || !dialog.status) && <Info size={48} />}
                                </div>
                                <h3>{dialog.status === 'error' ? 'Error' : dialog.status === 'success' ? 'Success' : 'Message'}</h3>
                                <p>{dialog.message}</p>
                                <button className="btn btn-primary" onClick={() => handleConfirm(true)}>OK</button>
                            </div>
                        )}

                        {dialog.type === 'confirm' && (
                            <div className="notification-content">
                                <div className="notification-icon question">
                                    <HelpCircle size={48} />
                                </div>
                                <h3>{dialog.title}</h3>
                                <p>{dialog.message}</p>
                                <div className="notification-actions">
                                    <button className="btn btn-secondary" onClick={() => handleConfirm(false)}>Cancel</button>
                                    <button className="btn btn-primary" onClick={() => handleConfirm(true)}>Confirm</button>
                                </div>
                            </div>
                        )}

                        {dialog.type === 'prompt' && (
                            <div className="notification-content">
                                <div className="notification-icon question">
                                    <HelpCircle size={48} />
                                </div>
                                <h3>{dialog.title}</h3>
                                <p>{dialog.message}</p>
                                <input
                                    type="text"
                                    className="input notification-input"
                                    autoFocus
                                    defaultValue={dialog.defaultValue}
                                    onKeyDown={(e) => {
                                        if (e.key === 'Enter') handleConfirm(e.target.value);
                                        if (e.key === 'Escape') handleConfirm(null);
                                    }}
                                    id="dialog-prompt-input"
                                />
                                <div className="notification-actions">
                                    <button className="btn btn-secondary" onClick={() => handleConfirm(null)}>Cancel</button>
                                    <button className="btn btn-primary" onClick={() => handleConfirm(document.getElementById('dialog-prompt-input').value)}>Submit</button>
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            )}
        </NotificationContext.Provider>
    );
};

