# RMS Frontend - Card Design System

## Red Accent Indicator Rules

### ✅ Cards WITH Red Accent (Left Border)
The **4px solid red left border** is applied ONLY to:

1. **Dashboard Stat Cards** (`.stat-card`)
   - Total Requests
   - Pending Requests
   - Approved Requests
   - Rejected Requests
   - Any KPI/metric card showing numeric data

### ❌ Cards WITHOUT Red Accent
Regular content cards do NOT have the red accent:

1. **Filter Cards** (`.filters-card`)
   - Advanced filter sections
   - Search and filter controls

2. **Table Cards** (`.table-card`)
   - Data tables
   - Request lists
   - Employee lists

3. **Chart Cards** (`.chart-card`)
   - Bar charts
   - Pie charts
   - Line charts

4. **Form Cards**
   - Request forms
   - Employee forms
   - Any input forms

## Implementation

### Stat Card (WITH accent)
```jsx
<Card hoverable={false} className="stat-card">
    <div className="stat-content">
        <div className="stat-label">Total Requests</div>
        <div className="stat-value">42</div>
    </div>
    <div className="stat-icon-container">
        <BarChart3 size={28} />
    </div>
</Card>
```

### Regular Card (NO accent)
```jsx
<Card className="filters-card">
    {/* Filter content */}
</Card>

<Card className="table-card">
    {/* Table content */}
</Card>
```

## Visual Hierarchy

**Stat Cards:**
- Red accent line (4px left border)
- Horizontal layout
- Label (uppercase, gray)
- Large numeric value (bold, dark)
- Icon container (light red background)

**Content Cards:**
- Clean white background
- No accent line
- Soft shadow
- Rounded corners
- Standard padding
