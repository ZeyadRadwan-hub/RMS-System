# RMS Quick Start

راجع [HOW_TO_RUN.md](HOW_TO_RUN.md) للتشغيل الكامل والهجرات والقيود الحالية. الأوامر من المجلد الأساسي للمشروع.

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -d RMS -E -b -i 'RMS-BACKEND\Database\001_baseline_validation.sql'
dotnet restore RMS-BACKEND.slnx
dotnet build RMS-BACKEND.slnx --no-restore
dotnet run --project RMS-BACKEND/RMS-BACKEND.csproj --no-build --launch-profile http
```

في terminal آخر:

```powershell
npm --prefix FrontEnd ci
npm --prefix FrontEnd run dev -- --host localhost --port 5173 --strictPort
```

- Frontend: http://localhost:5173/login
- API: http://localhost:5190/api
- Swagger في Development: http://localhost:5190
- DB: `RMS` على `(localdb)\MSSQLLocalDB`، Windows Authentication.

في قاعدة `RMS` التجريبية بعد إعادة التعيين، استخدم كلمة المرور المشتركة التي حددها المستخدم في المحادثة. ملف provisioning القديم صالح لأكواد الحسابات فقط. هذه السياسة لا تصلح للإنتاج.

## API

Login:

```http
POST http://localhost:5190/api/auth/login
Content-Type: application/json

{"code":"<employee-code>","password":"<current-test-password>"}
```

استخدم token الناتج، وليس employeeId أو role من العميل:

```http
GET http://localhost:5190/api/transactions/my-requests?page=1&pageSize=100
Authorization: Bearer <session-token>
```

القوائم تعيد `items/page/pageSize/totalCount/hasNext`، مع حد pageSize أقصى 200.
أنواع الطلبات مأخوذة من lookup API: 1 Sick Leave، 2 Annual Leave، 3 Half Day، 4 Bonus Leave، 5 Unpaid Leave.
Sick Leave يُرسل multipart مع مستند طبي. التعديل مسموح للمالك في Pending فقط. المدير يراجع تابعيه؛ HR وBoard يُطبقان انتقالات الحالة وصلاحيات القرار في الخادم.

## Verification

```powershell
dotnet test RMS-BACKEND.slnx --no-restore
npm --prefix FrontEnd test
npm --prefix FrontEnd run lint
npm --prefix FrontEnd run build
```

الحالة التفصيلية في `RMS_Repair_Report.md`، والتتبع الفردي في `RMS_Repair_Traceability.csv`. مراجعة التشغيل لا تعني اكتمال جميع البنود؛ راجع مصفوفة التحقق والاختبارات غير المنفذة. قبل تغييرات DB اتبع النسخ الاحتياطي والهجرات في MIGRATIONS.md.
