import React, { useState, useRef, useEffect } from 'react';
import { X, ChevronDown } from 'lucide-react';
import './MultiSelect.css';

const MultiSelect = ({
    options = [],
    selected = [],
    onChange,
    placeholder = "Select...",
    label,
    displayKey = "name",
    valueKey = "id"
}) => {
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef(null);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const toggleOption = (value) => {
        const newSelected = selected.includes(value)
            ? selected.filter(item => item !== value)
            : [...selected, value];
        onChange(newSelected);
    };

    const removeItem = (value, e) => {
        e.stopPropagation();
        onChange(selected.filter(item => item !== value));
    };

    const getDisplayName = (value) => {
        const option = options.find(opt =>
            (typeof opt === 'object' ? opt[valueKey] : opt) === value
        );
        return option ? (typeof option === 'object' ? option[displayKey] : option) : value;
    };

    return (
        <div className="multiselect-container" ref={dropdownRef}>
            {label && <label className="label">{label}</label>}

            <div
                className={`multiselect-input ${isOpen ? 'open' : ''}`}
                onClick={() => setIsOpen(!isOpen)}
            >
                <div className="multiselect-value">
                    {selected.length === 0 ? (
                        <span className="multiselect-placeholder">{placeholder}</span>
                    ) : (
                        <span className="multiselect-count">{selected.length} selected</span>
                    )}
                </div>
                <ChevronDown
                    size={16}
                    className={`multiselect-arrow ${isOpen ? 'rotate' : ''}`}
                />
            </div>

            {isOpen && (
                <div className="multiselect-dropdown">
                    {options.length === 0 ? (
                        <div className="multiselect-empty">No options available</div>
                    ) : (
                        options.map((option) => {
                            const value = typeof option === 'object' ? option[valueKey] : option;
                            const display = typeof option === 'object' ? option[displayKey] : option;
                            const isSelected = selected.includes(value);

                            return (
                                <div
                                    key={value}
                                    className={`multiselect-option ${isSelected ? 'selected' : ''}`}
                                    onClick={() => toggleOption(value)}
                                >
                                    <input
                                        type="checkbox"
                                        checked={isSelected}
                                        onChange={() => { }}
                                        className="multiselect-checkbox"
                                    />
                                    <span>{display}</span>
                                </div>
                            );
                        })
                    )}
                </div>
            )}

            {/* Selected chips */}
            {selected.length > 0 && (
                <div className="multiselect-chips">
                    {selected.map((value) => (
                        <div key={value} className="multiselect-chip">
                            <span>{getDisplayName(value)}</span>
                            <button
                                type="button"
                                onClick={(e) => removeItem(value, e)}
                                className="multiselect-chip-remove"
                            >
                                <X size={14} />
                            </button>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};

export default MultiSelect;
