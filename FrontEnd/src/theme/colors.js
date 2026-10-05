// RMS Leave Management System - Color Palette
// Exact theme specification with red gradient primary colors

export const colors = {
  // Primary Red Gradient
  primary: {
    deep: '#C4161C',
    bright: '#E53935',
    gradient: 'linear-gradient(135deg, #C4161C 0%, #E53935 100%)',
    hover: '#A01217',
    light: '#FF5252',
    lighter: '#FFCDD2',
  },

  // Background Colors
  background: {
    main: '#F8F9FB',
    card: '#FFFFFF',
    sidebar: '#FFFFFF',
    hover: '#F5F5F5',
  },

  // Text Colors
  text: {
    primary: '#1F2937',
    secondary: '#6B7280',
    light: '#9CA3AF',
    white: '#FFFFFF',
  },

  // Border Colors
  border: {
    light: '#E5E7EB',
    medium: '#D1D5DB',
    dark: '#9CA3AF',
  },

  // Status Colors (for leave request statuses)
  status: {
    pending: '#FFA726',
    approved: '#66BB6A',
    rejected: '#EF5350',
    cancelled: '#78909C',
  },

  // Chart Colors (Red shades for ag-Grid charts)
  chart: {
    red1: '#C4161C',
    red2: '#D32F2F',
    red3: '#E53935',
    red4: '#EF5350',
    red5: '#FF5252',
    red6: '#FF7961',
    red7: '#FFCDD2',
  },

  // Utility Colors
  success: '#66BB6A',
  warning: '#FFA726',
  error: '#EF5350',
  info: '#42A5F5',
};

export default colors;
