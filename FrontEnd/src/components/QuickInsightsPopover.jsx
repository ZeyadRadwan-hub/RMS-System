import React, { useState, useRef, useCallback, useEffect, useId } from 'react';
import { Zap, Calendar, Briefcase, Users, CheckCircle2, AlertTriangle } from 'lucide-react';
import leaveBalanceService from '../services/leaveBalanceService';
import './QuickInsightsPopover.css';

/**
 * QuickInsightsPopover
 * Wraps an employee name cell with a hover-triggered floating popover
 * showing leave balance insights for decision-making.
 *
 * Props:
 *  - employeeId: number (required) — ID of the employee
 *  - employeeName: string — display name
 *  - request: object — the leave request being hovered (for date range + dept overlap)
 *  - allRequests: array — all requests in current view (for computing dept overlap)
 *  - children: ReactNode (optional) — custom trigger content
 */
const QuickInsightsPopover = ({ employeeId, employeeName, request, allRequests = [], children }) => {
    const [isVisible, setIsVisible] = useState(false);
    const [balanceData, setBalanceData] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const hoverTimerRef = useRef(null);
    const leaveTimerRef = useRef(null);
    const cacheRef = useRef({});
    const anchorRef = useRef(null);
    const popoverId = useId();

    // Cleanup timers on unmount
    useEffect(() => {
        return () => {
            clearTimeout(hoverTimerRef.current);
            clearTimeout(leaveTimerRef.current);
        };
    }, []);

    const fetchBalance = useCallback(async () => {
        if (!employeeId) return;

        // Use cache to avoid redundant API calls
        if (cacheRef.current[employeeId]) {
            setBalanceData(cacheRef.current[employeeId]);
            return;
        }

        try {
            setLoading(true);
            setError(null);
            const data = await leaveBalanceService.getBalance(employeeId);
            cacheRef.current[employeeId] = data;
            setBalanceData(data);
        } catch (err) {
            console.error('QuickInsights: Error fetching balance', err);
            setError('Could not load data');
        } finally {
            setLoading(false);
        }
    }, [employeeId]);

    const handleMouseEnter = useCallback(() => {
        clearTimeout(leaveTimerRef.current);
        hoverTimerRef.current = setTimeout(() => {
            setIsVisible(true);
            fetchBalance();
        }, 300); // 300ms debounce
    }, [fetchBalance]);

    const handleMouseLeave = useCallback(() => {
        clearTimeout(hoverTimerRef.current);
        leaveTimerRef.current = setTimeout(() => {
            setIsVisible(false);
        }, 150); // Short delay so user can move to popover
    }, []);

    // Compute department overlap: how many approved/pending requests from
    // the same department overlap with this request's date range
    const computeDeptOverlap = () => {
        if (!request || !allRequests.length) return 0;

        const reqStart = new Date(request.startDate);
        const reqEnd = new Date(request.endDate);
        const reqDept = request.departmentName;

        return allRequests.filter(r => {
            if (r.id === request.id) return false; // Exclude this request
            if (r.employeeId === request.employeeId) return false; // Exclude same employee
            if (r.departmentName !== reqDept) return false; // Must be same dept
            // Only count approved or pending requests
            if (![1, 2, 3].includes(r.statusID)) return false;
            // Check date range overlap
            const rStart = new Date(r.startDate);
            const rEnd = new Date(r.endDate);
            return rStart <= reqEnd && rEnd >= reqStart;
        }).length;
    };

    const formatTenure = (dateStr) => {
        if (!dateStr) return '—';
        const start = new Date(dateStr);
        const now = new Date();
        const diffMs = now - start;
        const years = Math.floor(diffMs / (365.25 * 24 * 60 * 60 * 1000));
        const months = Math.floor((diffMs % (365.25 * 24 * 60 * 60 * 1000)) / (30.44 * 24 * 60 * 60 * 1000));
        if (years > 0) return `${years}y ${months}m`;
        return `${months}m`;
    };

    const deptOverlap = isVisible ? computeDeptOverlap() : 0;

    return (
        <div
            className="qi-anchor"
            ref={anchorRef}
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
        >
            <button type="button" className="qi-trigger" aria-expanded={isVisible}
                aria-controls={popoverId}
                onClick={() => {
                    if (!isVisible) void fetchBalance();
                    setIsVisible(!isVisible);
                }}
                onKeyDown={(event) => { if (event.key === 'Escape') setIsVisible(false); }}>
                {children || <span className="qi-employee-name">{employeeName}</span>}
            </button>

            {isVisible && (
                <div id={popoverId} className="qi-popover" role="status">
                    {/* Header */}
                    <div className="qi-header">
                        <div className="qi-header-icon">
                            <Zap size={14} />
                        </div>
                        <div className="qi-header-text">
                            <p className="qi-header-title">Quick Insights</p>
                            <p className="qi-header-sub">{employeeName}</p>
                        </div>
                    </div>

                    {loading ? (
                        <div className="qi-loading">
                            <div className="qi-loading-spinner" />
                            <span>Loading insights...</span>
                        </div>
                    ) : error ? (
                        <div className="qi-error">{error}</div>
                    ) : balanceData ? (
                        <>
                            {/* Stats Grid */}
                            <div className="qi-stats">
                                <div className="qi-stat">
                                    <div className="qi-stat-label">Remaining Balance</div>
                                    <div className={`qi-stat-value ${balanceData.leaveBalance > 5 ? 'qi-green' : balanceData.leaveBalance > 0 ? 'qi-orange' : 'qi-red'}`}>
                                        {balanceData.leaveBalance?.toFixed(1)}
                                        <span className="qi-stat-unit">days</span>
                                    </div>
                                </div>
                                <div className="qi-stat">
                                    <div className="qi-stat-label">Leave Used</div>
                                    <div className="qi-stat-value qi-blue">
                                        {balanceData.leaveUsed?.toFixed(1)}
                                        <span className="qi-stat-unit">days</span>
                                    </div>
                                </div>
                                <div className="qi-stat">
                                    <div className="qi-stat-label">Tenure</div>
                                    <div className="qi-stat-value">
                                        {formatTenure(balanceData.dateOfEmployment)}
                                    </div>
                                </div>
                                <div className="qi-stat">
                                    <div className="qi-stat-label">Entitlement</div>
                                    <div className="qi-stat-value">
                                        {balanceData.annualLeaveEntitlement}
                                        <span className="qi-stat-unit">d/yr</span>
                                    </div>
                                </div>
                            </div>

                            {/* Department Overlap Alert */}
                            {request && (
                                <div className={`qi-dept-alert ${deptOverlap === 0 ? 'qi-clear' : ''}`}>
                                    {deptOverlap > 0 ? (
                                        <>
                                            <AlertTriangle size={14} />
                                            <span>{deptOverlap} team member{deptOverlap > 1 ? 's' : ''} also away on these dates</span>
                                        </>
                                    ) : (
                                        <>
                                            <CheckCircle2 size={14} />
                                            <span>No team overlap on these dates</span>
                                        </>
                                    )}
                                </div>
                            )}
                        </>
                    ) : null}
                </div>
            )}
        </div>
    );
};

export default QuickInsightsPopover;
