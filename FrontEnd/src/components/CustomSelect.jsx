import React, { useState, useRef, useEffect } from 'react';
import { ChevronDown, Check } from 'lucide-react';
import './CustomSelect.css';

const CustomSelect = ({
    options = [],
    value,
    onChange,
    placeholder = "Select option",
    label,
    className = ""
}) => {
    const [isOpen, setIsOpen] = useState(false);
    const containerRef = useRef(null);

    // Close when clicking outside
    useEffect(() => {
        const handleClickOutside = (event) => {
            if (containerRef.current && !containerRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const handleSelect = (optionValue) => {
        onChange(optionValue);
        setIsOpen(false);
    };

    const selectedOption = options.find(opt =>
        (typeof opt === 'object' ? opt.value : opt) === value
    );

    const displayValue = selectedOption
        ? (typeof selectedOption === 'object' ? selectedOption.label : selectedOption)
        : placeholder;

    return (
        <div className={`custom-select-container ${className}`} ref={containerRef}
            onKeyDown={(event) => { if (event.key === 'Escape') setIsOpen(false); }}>
            {label && <label className="custom-select-label">{label}</label>}

            <div className="custom-select-wrapper">
                <button
                    type="button"
                    className={`custom-select-trigger ${isOpen ? 'active' : ''}`}
                    onClick={() => setIsOpen(!isOpen)}
                    aria-label={label || placeholder}
                    aria-expanded={isOpen}
                    aria-haspopup="true"
                >
                    <span className={`selected-text ${!selectedOption ? 'placeholder' : ''}`}>
                        {displayValue}
                    </span>
                    <ChevronDown size={18} className={`select-chevron ${isOpen ? 'rotate' : ''}`} />
                </button>

                {isOpen && (
                    <div className="custom-select-dropdown">
                        {options.map((option, index) => {
                            const optValue = typeof option === 'object' ? option.value : option;
                            const optLabel = typeof option === 'object' ? option.label : option;
                            const isSelected = optValue === value;

                            return (
                                <button
                                    type="button"
                                    key={index}
                                    className={`custom-select-option ${isSelected ? 'selected' : ''}`}
                                    onClick={() => handleSelect(optValue)}
                                    aria-pressed={isSelected}
                                >
                                    <span>{optLabel}</span>
                                    {isSelected && <Check size={16} className="check-icon" />}
                                </button>
                            );
                        })}
                    </div>
                )}
            </div>
        </div>
    );
};

export default CustomSelect;
