# RMS Frontend - Stat Card Design Specifications

## ✅ Complete Implementation Summary

### Scope
Applied ONLY to containers/cards that:
- ✅ Appear on dashboard pages (Dashboard, Leave Balance)
- ✅ Include a red vertical accent line on the left
- ✅ Display numeric values or statistics

---

## Layout & Structure

### Card Content Alignment

#### 1. Label (Title)
- **Position**: Top-left area of the card
- **Style**: 
  - `text-transform: uppercase`
  - `font-size: 0.8125rem` (13px)
  - `font-weight: 500`
  - `color: #6B7280` (muted dark gray)
  - `letter-spacing: 0.5px`

#### 2. Numeric Value
- **Position**: Directly below the label
- **Style**:
  - `font-size: 2.25rem` (36px) - significantly larger than label
  - `font-weight: 700` (bold)
  - `color: #111827` (near-black for emphasis)
  - `line-height: 1`

#### 3. Icon
- **Position**: Right side of the card, vertically centered
- **Icon Container**:
  - `width: 72px`
  - `height: 72px`
  - `border-radius: 12px`
  - `background: rgba(196, 22, 28, 0.08)` (light red / soft pink tint)
- **Icon Style**:
  - Line-style icon (Lucide React)
  - `size: 30px`
  - `color: var(--color-primary-deep)` (primary red brand color)
  - `stroke-width: 2`

---

## Card Proportions & Spacing

### Dimensions
- **Min Height**: `130px`
- **Padding**: `1.5rem 2rem` (24px 32px) - comfortable, not cramped
- **Gap**: `1.5rem` (24px) between content and icon

### Grid Layout
```css
.stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1.5rem;
}
```

### Visual Balance
- ✅ Left content (label + number) takes flex: 1
- ✅ Right icon container is fixed 72px × 72px
- ✅ All cards in same row have equal height
- ✅ Consistent spacing between cards

---

## Responsive Behavior

### Desktop (> 1024px)
- Min card width: 280px
- Icon container: 72px
- Font size (value): 2.25rem (36px)
- Padding: 1.5rem 2rem

### Tablet (768px - 1024px)
- Min card width: 280px
- Icon container: 64px
- Font size (value): 2rem (32px)
- Padding: 1.25rem 1.5rem

### Mobile (< 768px)
- Min card width: 100%
- Icon container: 56px
- Font size (value): 1.75rem (28px)
- Padding: 1rem 1.25rem

### Small Mobile (< 480px)
- Icon container: 48px
- Font size (value): 1.5rem (24px)
- Padding: 0.875rem 1rem

---

## UX & Visual Consistency

### Visual Hierarchy
1. **Label** (small, uppercase, muted)
2. **Number** (large, bold, dark)
3. **Icon** (medium, contained, branded)

### Consistency Rules
✅ All statistic cards look identical in structure  
✅ Only differences: text, numeric value, icon type  
✅ Red accent line ONLY on stat cards  
✅ No accent on: filters, tables, charts, forms  

---

## Implementation Examples

### Dashboard Page
```jsx
<Card hoverable={false} className="stat-card">
    <div className="stat-content">
        <div className="stat-label">Total Requests</div>
        <div className="stat-value">{stats?.totalRequests || 0}</div>
    </div>
    <div className="stat-icon-container">
        <BarChart3 size={30} />
    </div>
</Card>
```

### Leave Balance Page
```jsx
<Card hoverable={false} className="stat-card">
    <div className="stat-content">
        <div className="stat-label">Available Balance</div>
        <div className="stat-value">{balance.leaveBalance?.toFixed(1) || 0}</div>
    </div>
    <div className="stat-icon-container">
        <BarChart3 size={30} />
    </div>
</Card>
```

---

## Pages Using Stat Cards

### Dashboard
- Total Requests (BarChart3)
- Pending (Clock)
- Approved (CheckCircle)
- Rejected (XCircle)

### Leave Balance
- Available Balance (BarChart3)
- Total Accrued (TrendingUp)
- Leave Used (TrendingDown)
- Annual Entitlement (Calendar)

---

## CSS Files Modified

1. **Card.css** - Global stat-card styles
2. **Dashboard.css** - Stats grid layout
3. **LeaveBalance.css** - Balance cards grid layout

---

## Design Principles Met

✅ Clear communication of key statistics at a glance  
✅ Strong visual hierarchy (Label → Number → Icon)  
✅ Comfortable spacing (not cramped)  
✅ Increased card width for better proportions  
✅ Equal height cards in same row  
✅ Consistent spacing across all stat cards  
✅ Responsive across all screen sizes  
✅ Professional enterprise appearance  
✅ Brand-consistent red accent and icons  

---

**Status**: ✅ 100% Complete - All specifications implemented
