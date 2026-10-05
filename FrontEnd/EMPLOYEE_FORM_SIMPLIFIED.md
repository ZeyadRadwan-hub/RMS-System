# تحديث Employee Form - النسخة المبسطة

## ✅ التعديلات المطبقة

### **الحقول الجديدة (مبسطة):**

#### 1. **Employee Code** (required)
- نص عادي
- مثال: `EMP001`
- **Disabled في Edit mode** (لا يمكن تغييره)

#### 2. **Full Name** (required)
- نص عادي
- مثال: `Ahmed Mohamed`

#### 3. **Department** (required)
- Dropdown:
  - IT
  - HR
  - Finance
  - Marketing
  - Operations

#### 4. **Date of Employment** (required)
- Date picker

#### 5. **Manager** (optional)
- **Dropdown للمديرين فقط**
- يجلب قائمة الموظفين اللي `isManager = true`
- يعرض: `Name (Department)`
- مثال: `Ahmed Mohamed (IT)`
- خيار: `No Manager (Top Level)` للموظفين بدون مدير

#### 6. **Employee Level** (required)
- Dropdown:
  - **A (21 days)**
  - **B (14 days)**
- فقط خيارين!

#### 7. **Annual Leave Entitlement** (auto-calculated)
- **Read-only** (لا يمكن تعديله)
- يتحسب أوتوماتيك بناءً على Level:
  - Level A → 21 days
  - Level B → 14 days
- رسالة: "Auto-calculated based on level"

#### 8. **Is Manager** (checkbox)
- Checkbox لتحديد إذا كان الموظف مدير
- Default: `false`

---

## ❌ **الحقول المحذوفة:**

- ❌ Email
- ❌ Manager ID (manual input) - استبدل بـ dropdown
- ❌ Employee Level C, D - فقط A و B
- ❌ Active Status - تم إزالته من Add/Edit

---

## 🔄 **الميزات الذكية:**

### **1. Auto-Calculate Leave Entitlement:**
```javascript
// عند اختيار Level:
Level A (ID: 1) → 21 days
Level B (ID: 2) → 14 days
```

### **2. Manager Dropdown:**
- يجلب فقط الموظفين اللي `isManager = true`
- يعرض الاسم والقسم
- Loading state أثناء الجلب

### **3. Validation:**
- Employee Code (required)
- Name (required)
- Department (required)
- Date of Employment (required)
- Employee Level (required)
- Manager (optional)

---

## 📊 **مثال على البيانات المرسلة:**

### **Add Employee:**
```json
{
  "code": "EMP001",
  "name": "Ahmed Mohamed",
  "departmentID": "1",
  "managerID": "5",
  "dateOfEmployment": "2024-01-15",
  "employeeLevelID": "1",
  "annualLeaveEntitlement": 21,
  "isManager": false
}
```

### **Edit Employee:**
```json
{
  "code": "EMP001",  // disabled, can't change
  "name": "Ahmed Mohamed Ali",  // updated
  "departmentID": "2",  // changed to HR
  "managerID": null,  // removed manager
  "dateOfEmployment": "2024-01-15",
  "employeeLevelID": "2",  // changed to B
  "annualLeaveEntitlement": 14,  // auto-updated to 14
  "isManager": true  // now a manager
}
```

---

## 🎯 **كيفية الاستخدام:**

### **Add Employee:**
1. اضغط "Add Employee"
2. املأ الحقول:
   - Code: `EMP001`
   - Name: `Ahmed Mohamed`
   - Department: اختر من القائمة
   - Date of Employment: اختر التاريخ
   - Manager: اختر مدير (أو اترك فارغ)
   - Level: اختر A أو B
   - ✅ Annual Leave يتحسب أوتوماتيك
   - Is Manager: ✓ إذا كان مدير
3. اضغط "Add Employee"

### **Edit Employee:**
1. اضغط Edit (✏️)
2. عدل البيانات:
   - Code: **لا يمكن تغييره**
   - Name: عدل الاسم
   - Department: غير القسم
   - Manager: غير المدير
   - Level: غير المستوى
   - ✅ Annual Leave يتحدث أوتوماتيك
   - Is Manager: غير الحالة
3. اضغط "Save Changes"

---

## 💡 **الميزات الإضافية:**

### **Manager Dropdown:**
- يعرض فقط المديرين
- Format: `Name (Department)`
- مثال:
  ```
  Ahmed Mohamed (IT)
  Sara Ali (HR)
  Mohamed Hassan (Finance)
  ```

### **Auto-Calculate:**
- عند اختيار Level A → 21 days
- عند اختيار Level B → 14 days
- الحقل disabled (لا يمكن تعديله يدوياً)

### **Validation:**
- كل الحقول المطلوبة تظهر رسالة خطأ
- Employee Code لا يمكن تغييره في Edit

---

## 🚀 **الحالة:**

| Feature | Status |
|---------|--------|
| Simplified Fields | ✅ |
| Manager Dropdown | ✅ |
| Auto-Calculate Leave | ✅ |
| Is Manager Checkbox | ✅ |
| Level A/B Only | ✅ |
| Validation | ✅ |
| Add Mode | ✅ |
| Edit Mode | ✅ |

---

**كل شيء جاهز!** 🎉

الـ Form الآن مبسط وسهل الاستخدام مع كل الميزات المطلوبة.
