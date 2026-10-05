import React, { useState, useRef, useEffect } from 'react';
import { Calendar as CalendarIcon, ChevronLeft, ChevronRight } from 'lucide-react';
import './CustomDatePicker.css';
import { formatDateOnly, parseDateOnly } from '../utils/dateOnly';

const CustomDatePicker = ({ value, onChange, label, placeholder = "Select date" }) => {
    const [isOpen, setIsOpen] = useState(false);
    const [viewSelection, setViewSelection] = useState(null);
    const viewDate = viewSelection && viewSelection.value === value
        ? viewSelection.date
        : (value ? parseDateOnly(value) : new Date());
    const containerRef = useRef(null);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (containerRef.current && !containerRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const daysInMonth = (month, year) => new Date(year, month + 1, 0).getDate();
    const firstDayOfMonth = (month, year) => new Date(year, month, 1).getDay();

    const handleDateSelect = (day) => {
        const selectedDate = new Date(viewDate.getFullYear(), viewDate.getMonth(), day);
        onChange(formatDateOnly(selectedDate));
        setIsOpen(false);
    };

    const changeMonth = (offset) => {
        setViewSelection({ value, date: new Date(viewDate.getFullYear(), viewDate.getMonth() + offset, 1) });
    };

    const monthNames = ["January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"];

    const renderCalendar = () => {
        const month = viewDate.getMonth();
        const year = viewDate.getFullYear();
        const days = daysInMonth(month, year);
        const firstDay = firstDayOfMonth(month, year);
        const calendarDays = [];

        // Empty slots for previous month
        for (let i = 0; i < firstDay; i++) {
            calendarDays.push(<div key={`empty-${i}`} className="calendar-day empty"></div>);
        }

        // Days of current month
        for (let d = 1; d <= days; d++) {
            const isSelected = value === formatDateOnly(new Date(year, month, d));
            const isToday = new Date().toDateString() === new Date(year, month, d).toDateString();

            calendarDays.push(
                <button
                    type="button"
                    key={d}
                    className={`calendar-day ${isSelected ? 'selected' : ''} ${isToday ? 'today' : ''}`}
                    onClick={() => handleDateSelect(d)}
                    aria-label={formatDateOnly(new Date(year, month, d))}
                    aria-pressed={isSelected}
                >
                    {d}
                </button>
            );
        }

        return calendarDays;
    };

    return (
        <div className="custom-datepicker-container" ref={containerRef}
            onKeyDown={(event) => { if (event.key === 'Escape') setIsOpen(false); }}>
            {label && <label className="custom-datepicker-label">{label}</label>}

            <div className="custom-datepicker-wrapper">
                <button
                    type="button"
                    className={`custom-datepicker-trigger ${isOpen ? 'active' : ''}`}
                    onClick={() => {
                        setViewSelection(null);
                        setIsOpen(!isOpen);
                    }}
                    aria-label={label || placeholder}
                    aria-expanded={isOpen}
                >
                    <span className={`selected-date ${!value ? 'placeholder' : ''}`}>
                        {value ? parseDateOnly(value).toLocaleDateString() : placeholder}
                    </span>
                    <CalendarIcon size={18} className="calendar-icon" />
                </button>

                {isOpen && (
                    <div className="calendar-popup">
                        <div className="calendar-header">
                            <button type="button" aria-label="Previous month" onClick={(e) => { e.stopPropagation(); changeMonth(-1); }} className="nav-btn"><ChevronLeft size={16} /></button>
                            <span className="month-year">{monthNames[viewDate.getMonth()]} {viewDate.getFullYear()}</span>
                            <button type="button" aria-label="Next month" onClick={(e) => { e.stopPropagation(); changeMonth(1); }} className="nav-btn"><ChevronRight size={16} /></button>
                        </div>
                        <div className="calendar-weekdays">
                            {['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'].map(d => <div key={d} className="weekday">{d}</div>)}
                        </div>
                        <div className="calendar-grid">
                            {renderCalendar()}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default CustomDatePicker;
