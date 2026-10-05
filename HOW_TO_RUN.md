# تشغيل RMS محليًا

آخر مراجعة: 2026-10-05. تُشغّل الأوامر التالية من مجلد المشروع الأساسي الذي يحتوي `RMS-BACKEND.slnx` و`FrontEnd`.

## المتطلبات وقاعدة البيانات

- .NET 10 SDK وNode.js/npm وSQL Server LocalDB.
- قاعدة الاختبار المصرح بها: `RMS` على `(localdb)\MSSQLLocalDB`، باستخدام Windows Authentication.
- إعداد الاتصال موجود في `RMS-BACKEND/appsettings.json`؛ تشغيل الخادم لا يُطبّق تغييرات schema تلقائيًا.
- قبل أي تغيير schema أو بيانات جوهري: نسخة COPY_ONLY مع CHECKSUM ثم RESTORE VERIFYONLY. تعليمات الهجرات وترتيبها في `RMS-BACKEND/Database/MIGRATIONS.md`. لا تُعد تشغيل هجرة مطبقة.
- لا تستخدم قاعدة أخرى أو تستعد نسخة فوق RMS ضمن اختبارات هذه المهمة.

فحص read-only للعقد الحالي:

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -d RMS -E -b -i 'RMS-BACKEND\Database\001_baseline_validation.sql'
```

## تشغيل Backend

```powershell
dotnet restore RMS-BACKEND.slnx
dotnet build RMS-BACKEND.slnx --no-restore
dotnet run --project RMS-BACKEND/RMS-BACKEND.csproj --no-build --launch-profile http
```

عنوان التطوير HTTP: **http://localhost:5190**، ومسار API: **http://localhost:5190/api**.
Swagger متاح في Development على جذر عنوان الخادم. ملف launchSettings يحتوي أيضًا profile باسم `https` على https://localhost:7167. لم يُثبت مسار شهادة HTTPS في اختبار المتصفح الحالي.

## تشغيل Frontend

افتح terminal آخر من المجلد الأساسي:

```powershell
npm --prefix FrontEnd ci
npm --prefix FrontEnd run dev -- --host localhost --port 5173 --strictPort
```

افتح **http://localhost:5173/login**. اختيار strictPort يمنع انتقال Vite بصمت إلى port غير موجود في إعداد CORS.

`FrontEnd/src/services/api.js` يستخدم `VITE_API_BASE_URL` إن وُجد؛ وإلا يستخدم http://localhost:5190/api في التطوير و`/api` في production. timeout يساوي 15 ثانية. إعداد CORS يسمح بالـorigins المعلنة في appsettings؛ إذا احتجت عنوانًا مختلفًا، حدّث إعداد البيئة بصورة متطابقة في الطرفين.

## الدخول ومسارات العمل

في قاعدة `RMS` التجريبية الحالية أعاد المستخدم تعيين كلمات مرور الحسابات الخمسة إلى قيمة مشتركة نصية، أرسلها في المحادثة. ملف `RMS_20261004_credentials.tsv` القديم لا يحتوي كلمات المرور الحالية؛ يمكن استخدامه لمعرفة الأكواد فقط. لا تُنقل سياسة كلمة المرور المشتركة والنص الصريح إلى الإنتاج. لتشغيل اختبار المتصفح المحلي، ضع القيمة الحالية في متغير `RMS_TEST_PASSWORD` داخل جلسة PowerShell دون إضافتها إلى المستودع.

Login يعيد token للجلسة. أرسل `Authorization: Bearer <session-token>` في الطلبات المحمية؛ الهوية والدور يُحددهما الخادم. headers القديمة X-Employee-Id وX-Employee-Role ليست وسيلة مصادقة.

- Employee: My Requests وLeave Balance وProfile.
- Manager: مسارات الموظف وTeam Requests لتابعيه.
- HR: Employees وAll Requests، وطلباته الشخصية تُراجع بواسطة Board.
- Board: HR Requests وHistory وبيانات المؤسسة وفق صلاحيات API.
- Sick Leave يحتاج مستندًا طبيًا عبر multipart؛ لا تُرسل File داخل JSON.
- معاملات all/my/team/filter تعيد `items/page/pageSize/totalCount/hasNext`، بحد أقصى 200 عنصر في الصفحة.
- Carryover يحتاج سياسة تجارية قبل تنفيذه؛ راجع التقرير لحالة كل بند.

## التحقق

```powershell
dotnet test RMS-BACKEND.slnx --no-restore
npm --prefix FrontEnd test
npm --prefix FrontEnd run lint
npm --prefix FrontEnd run build
npm --prefix FrontEnd audit --audit-level=low
```

أوامر اختبارات المتصفح والأدلة في `RMS_Test_Evidence/acceptance_2026-10-05.md`.
هذه البوابات لا تُغني عن التحقق من كل مسار في `RMS_Verification_Matrix.csv`.

## تشخيص التشغيل

عند فشل SQL، تحقق من instance وقاعدة RMS واسم حساب Windows ونص الخطأ قبل تغيير إعدادات الدخول. عند انشغال port، حدّد العملية المالكة قبل إيقافها. عند 401 أعد تسجيل الدخول؛ عند 403 راجع دور الحساب وملكية الطلب. عند فشل الرصيد، استخدم Retry بعد عودة الاتصال.

لـproduction: اضبط HTTPS والـreverse proxy ليخدم `/api` أو اضبط VITE_API_BASE_URL وقت البناء، وعيّن اتصال DB وCORS المناسبين. نشر production لم يُختبر في هذه المهمة.
