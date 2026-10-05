# دليل تشغيل وإصلاح RMS — 2026-10-05

## تغيير كلمة المرور المشتركة الأخير — تحقق بيانات فقط

- بناءً على قرار المستخدم، حُدّثت كلمات مرور موظفي الاختبار الخمسة إلى قيمة نصية مشتركة جديدة؛ لا توضع القيمة في هذا التقرير أو GitHub.
- نسخة ما قبل التغيير `RMS_20261005_pre_shared_password_refresh.bak` اجتازت COPY_ONLY/CHECKSUM وRESTORE VERIFYONLY.
- التعديل جرى على `RMS` وحدها داخل transaction واحدة: 5 قيم تغيرت، و91 جلسة نشطة أُلغيت دون حذف صفوفها. فحص ما بعد التنفيذ: Employees=5، DistinctPasswords=1، طول القيمة=10، Transactions=4، MedicalDocuments=0، RequestDecisionAudit=2، ActiveSessions=0.
- لم يُشغّل اختبار دخول أو Backend/Frontend بعد هذا التغيير التزامًا بطلب المستخدم «من غير Test». نتائج الاختبارات أدناه تخص الكود/البيانات قبل تغيير هذه القيمة؛ لا تثبت التشغيل بعدها.
- تبقى APP-BT-007 وAPP-BT-008 وDB-BT-003 مفتوحة أمنيًا لأن التخزين نص صريح والكلمة مشتركة.

## إعادة تحقق بعد توجيه عدم استخدام Hash لكلمات المرور

- `dotnet test RMS-BACKEND.Tests/RMS-BACKEND.Tests.csproj --no-restore --verbosity quiet`: Passed 114، Failed 0، Skipped 0.
- `npm --prefix FrontEnd test`: Passed 8، Failed 0. `npm --prefix FrontEnd run lint` و`npm --prefix FrontEnd run build`: Exit 0.
- `browser_smoke.py` على Edge headless: Exit 0؛ دخول الأدوار والتنقل، Annual/Sick/PDF، الموافقات، CRUD الموظف التجريبي، History CSV وProfile. خادم الاختبار أُوقف بعده.
- SQL بعد التنظيف: Employees=5، Transactions=4، MedicalDocuments=0، RequestDecisionAudit=2، وكلمات المرور ما زالت موحدة. لم تُنفذ هجرة كلمات مرور جديدة.
- `RMS_20261005_pre_hash_rotation.bak`: COPY_ONLY/CHECKSUM وRESTORE VERIFYONLY نجحا؛ أُخذت قبل قرار المستخدم الأخير ولا تعني أن هجرة Hash نُفذت. أُزيلت أداة الترحيل القديمة من المشروع؛ APP-BT-007/008 وDB-BT-003 لا تزال Open.

هذه الأدلة تخص نسخة الاختبار على `(localdb)\MSSQLLocalDB`، قاعدة `RMS`، Windows Authentication للحساب `DASH\Zeyad Radwan`. لم تُستخدم قاعدة أخرى. رُفعت نسخة العمل إلى `https://github.com/ZeyadRadwan-hub/RMS-System` على `main` في commit `59d4707`، والبنود الأمنية المفتوحة موثقة ولا توجد شهادة جاهزية إنتاج. التقريرين الأصليين في `Reports/` لم يتغيرا.

## فحص حديث يحتاج متابعة

- بعد إصلاح ربط الحالات بـStatus ID: اختبارات Frontend ‏8/8، وlint/build ناجحان. محاولة `browser_status_filters.py` بالمصادقة الحقيقية توقفت عند `Login failed`؛ إصدار UI mock معزول اجتاز أربع صفحات وفلاتر الحالات والوسوم كلها. كشف اعتراض بطاقة النتائج لقائمة History ثم ثبت إصلاحه بنفس الاختبار red→green. هذا يثبت الواجهة، لا تكامل المصادقة الحقيقي.
- أُزيل تجاوز `Pass@1234` من `AuthController.Login` بناءً على توضيح المستخدم. اختبار API أكد 401 لهذه القيمة و200 لكلمة الحساب المخزنة؛ NEW-007 الآن Fixed & Verified.
- بطلب المستخدم، غيّر سكربت 009 كلمات مرور موظفي الاختبار الخمسة إلى نص صريح مشترك دون نشر القيمة في المستودع؛ 5/5 مطابقة، و242 جلسة نشطة أُلغيت وقت التغيير. نسخة `RMS_20261005_pre_plaintext_passwords.bak` اجتازت VERIFYONLY. هذا يُعيد فتح مخاطر APP-BT-007/008 وDB-BT-003 أمنيًا على نسخة الاختبار.
- `browser_shared_password.py` اجتاز دخول `auth/me` وخروج الحسابات الخمسة بالقيمة الجديدة؛ `browser_smoke.py` اجتاز المسارات الكاملة بعد التغيير ونظف صفوفه إلى Employees=5 وTransactions=4 وMedicalDocuments=0 وRequestDecisionAudit=2. Backend كامل 114/114؛ Frontend 8/8، lint/build ناجحان.
- فحص القراءة الأخير وجد Employees=5 وTransactions=4 وMedicalDocuments=0 وRequestDecisionAudit=2؛ خط الأساس السابق كان Transactions=3 وAudit=0. المعاملة الإضافية Id=881 بتاريخ 2026-10-06؛ لم ينشئها اختبار فلاتر الحالة، ومصدر التغيير غير مثبت. تُحفظ كما هي إلى أن يُعرف مصدرها.

## نسخة قاعدة البيانات وسلامتها

- قبل اختبار recovery: `RMS_20261005_pre_lookup_recovery_183158.bak`، COPY_ONLY/CHECKSUM وRESTORE VERIFYONLY نجحا. `lookup_recovery.sql` حذف lookup غير مستخدمة (status 6/type 3) داخل transaction فقط، وعدّل اسم type 2 مؤقتًا للتحقق من عدم استبدال القيم الموجودة. setup استعاد الصفين ولم يغير الاسم الموجود، والتشغيل الثاني لم يغير أي lookup. ROLLBACK أعاد الصفوف الأصلية؛ مقارنات EXCEPT في الاتجاهين لكل Employees/Transactions/Statuses/TransactionTypes/EmployeeLevels نجحت. لا يشمل هذا اختبار تثبيت قاعدة جديدة أو restore فعليًا.

- قبل الهجرة 007: `RMS_20261005_pre_decision_audit.bak` في `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/`، `BACKUP DATABASE ... WITH COPY_ONLY, INIT, CHECKSUM` نجح، ثم `RESTORE VERIFYONLY ... WITH CHECKSUM` أعاد `The backup set on file 1 is valid`.
- قبل الهجرة 008: `RMS_20261005_pre_hierarchy.bak` بالمجلد نفسه، ونجح التحقق ذاته. فحص سلسلة المدراء قبلها أعاد 0 دورة.
- توجد كذلك نسخ 2026-10-04 قبل الإصلاح وتدوير كلمات المرور والقيود والتداخل والملفات الطبية وحماية الدخول، جميعها خارج OneDrive ومتحقق منها بـ`RESTORE VERIFYONLY`.
- أُخذت نسخة إضافية قبل إعادة اختبار idempotency: `RMS_20261005_pre_setup_idempotence.bak`، ونجح `RESTORE VERIFYONLY ... WITH CHECKSUM`.
- لم يحدث restore drill فعلي: المستخدم حصر الاختبارات في قاعدة `RMS` وحدها، ولا يجوز استعادة النسخة فوقها فقط لإثبات صلاحية الاستعادة. `VERIFYONLY` لا يثبت سلامة استعادة كاملة.
- آخر فحص بعد browser workflow: Employees=5، Transactions=3، MedicalDocuments=0، RequestDecisionAudit=0، وصفوف `RMS_AUTOTEST_%` للموظفين والطلبات=0. هذه أعداد بيانات البداية الأصلية، والمستندات/القرارات كانت صفرًا قبل التجارب الجديدة. كل صف تجريبي حُذف بالمعرف/الـmarker الدقيق؛ لم تُحذف صفوف مستخدم حقيقية.

## اختبارات أحمر → أخضر مثبتة

| الحالة | قبل الإصلاح | بعد الإصلاح |
| --- | --- | --- |
| ملف طبي من My Requests | HTTP 400: `Sick leave requires a medical document` رغم اختيار PDF | HTTP 200، قائمة المستندات والتنزيل يرجعان PDF |
| سجل صاحب القرار | `Invalid object name 'dbo.RequestDecisionAudit'` | سجل actor=1، الحالة 1→3، والاختبار ينجح |
| 50 إنشاءً مستقلًا بالتزامن | 45 من 50 رجعت HTTP 500 | 50/50 ناجحة ومعرفات مختلفة |
| قراران نهائيان متسابقان | قبول القرارين (2/2) | قبول قرار واحد فقط وسجل قرار واحد |
| Pending HR في Dashboard | `pendingRequests=1` مع طلب Pending HR موجود | `pendingRequests=2` للمجموعة الأصلية؛ اختبار معزول على الموظف 4 ينجح |
| أرصدة وطلبات بكميات كبيرة | أرصدة كل موظف نفذت استعلامين وقائمة الطلبات غير محدودة | batch للأرصدة (≤3 أوامر في اختبار 5+ موظفين) وpagination للطلبات بحد أقصى 200 صف؛ قياس logical reads ممثل لم يُجرَ |
| دورة إدارية 1→4→1 | SQL UPDATE قُبل، ثم رُجع ضمن test transaction | SQL يرفض، وAPI يعيد 400 دون تعديل |
| فلترة فترة متداخلة | الطلب 28/9–3/10 يختفي من فلتر أكتوبر | unit test يثبت ظهوره وحدود التقاطع |

## بوابات البناء والتشغيل

| الأمر | آخر نتيجة مؤكدة |
| --- | --- |
| `dotnet test RMS-BACKEND.slnx --no-restore --logger "console;verbosity=minimal"` | 112 Passed، 0 Failed، 0 Skipped؛ بعد إصلاح DTO |
| `npm --prefix FrontEnd test` | 5 Passed، 0 Failed |
| `npm --prefix FrontEnd run lint` | Exit 0، 0 errors، 0 warnings |
| `npm --prefix FrontEnd run build` | Exit 0، main JS ~309KB وDashboard ~379KB، دون تحذير chunk >500KB |
| `npm --prefix FrontEnd audit --audit-level=low` | `found 0 vulnerabilities` بعد تحديث/إزالة الحزم |

اختبار المتصفح شغّل Backend على port 5190 وVite على 5173 ضد RMS الفعلية، باستخدام Edge headless و`RMS_Test_Evidence/browser_smoke.py`. مساراته التي اجتازت: دخول Employee وHR وManager وحساب اختباري؛ إنشاء Annual وSick مع PDF؛ قائمة وتنزيل PDF؛ navigation للهاتف 390px؛ استخدام CustomSelect بالكيبورد؛ إنشاء موظف وتعديله ونقله لقسم/مدير آخر وأرشفته؛ موافقة Manager ثم HR؛ موافقة HR المباشرة وBoard على طلب آخر؛ تصدير History CSV وفتح Profile. لا تُطبع كلمات المرور أو الرموز أو محتوى الموظفين في السجل. اختبار browser الأخير Exit 0؛ التنظيف اللاحق أكد الأعداد الأصلية.

أُعيد تشغيل `database-setup.sql` على قاعدة RMS بعد النسخة الاحتياطية أعلاه؛ لم يغيّر أي صف، وبقيت الأعداد Employees=5، Transactions=3، Statuses=11، EmployeeLevels=2، TransactionTypes=5. هذا يثبت إعادة التشغيل الآمن على قاعدة مأهولة، لكنه لا يثبت استرجاع lookup مفقود في قاعدة جديدة.

أمر إعادة الاختبار (يحتاج Python Playwright المثبتة في مجلد الأدوات المحلي المشار إليه في السكربت):

```powershell
$env:PYTHONPATH='C:\Users\workstation\AppData\Local\RMS-Repair-Tools\python-playwright'
python 'C:\Users\workstation\.codex\skills\webapp-testing\scripts\with_server.py' `
  --server "dotnet run --project RMS-BACKEND/RMS-BACKEND.csproj --no-build --launch-profile http" --port 5190 `
  --server "npm --prefix FrontEnd run dev -- --host 127.0.0.1 --port 5173" --port 5173 -- `
  'C:\Users\workstation\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'RMS_Test_Evidence/browser_smoke.py'
```

## حدود الإثبات الحالية

- Carryover مؤجل لمراجعة الفريق بطلب المستخدم بتاريخ 2026-10-05؛ لا يُحسب هذا البند Fixed.
- browser_quick_insights.py فشل أولًا: aria-expanded=true لكن .qi-popover مخفية على touch بسبب display:none. بعد إزالة القاعدة اجتاز ظهور الرصيد باللمس وEnter والإغلاق بـEscape وربط aria-controls/role=status. هذا لا يثبت تشغيل قارئ شاشة فعلي.

آخر suite إضافية `browser_recovery_filters.py` اجتازت جميع فلاتر Employees (الأقسام الأربعة، A/B، الاسم والكود، Active/Inactive/All)، مع fixture للموظف inactive في الاستجابة فقط دون كتابة DB. اجتازت حالات HTTP 500 وconnectionfailed لأرصدة الموظفين، مع خطأ ظاهر دون نافذة أصفار، ثم فتح التفاصيل بنجاح عند استعادة الاتصال. اجتازت Leave Balance بنفس الفشلين ثم Retry الذي يعيد My Leave Balance ويزيل الخطأ. لتكرارها استبدل اسم browser_smoke.py في الأمر أعلاه بـbrowser_recovery_filters.py، واستخدم `--host localhost --port 5173 --strictPort` لتطابق CORS.

اختبارات DTO: ثلاثة اختبارات أولية فشلت قبل إضافة القواعد، ثم اجتازت. أُضيفت 18 حالة حدود غير صالحة وحالة حدود صحيحة، وخمس حالات JSON عبر API تثبت HTTP 400 دون كتابة بيانات بعلامة الاختبار. اكتشف اختبار التكامل منع موافقات صحيحة بسبب إلزام ID في body؛ صُحح لأن ID يُؤخذ من route، ثم اجتاز الاختبار الكامل 112/112. APP-BT-020 يبقى غير مغلق حتى استكمال كل حدود الفلاتر وmultipart غير الصالح.

لم يُجرَ اختبار كل إجراء في كل شاشة بعد. `RMS_Verification_Matrix.csv` يسرد المنفذ وغير المنفذ دون اعتبار عرض صفحة أو build دليلًا كافيًا. سياسة carryover التجارية غير محددة؛ سُئل مالك النظام عن الحد والانتهاء، وبقي `APP-BT-027` محجوبًا. أُثبتت البنية المحدودة لـN+1/pagination، لكن لا توجد قياسات logical reads على حجم ممثل، ولا اختبار إنشاء قاعدة جديدة أو restore drill بسبب قيد قاعدة `RMS` وحدها. استرجاع lookup المفقود داخل RMS أُثبت منفصلًا باختبار transaction وrollback الموضح أعلاه. لذلك هذا سجل تقدم لا شهادة اكتمال أو جاهزية إنتاج.
