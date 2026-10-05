# دليل تشغيل وإصلاح RMS — 2026-10-05

هذه الأدلة تخص نسخة الاختبار على `(localdb)\MSSQLLocalDB`، قاعدة `RMS`، Windows Authentication للحساب `DASH\Zeyad Radwan`. لم تُستخدم قاعدة أخرى. مجلد المشروع ليس Git worktree، لذلك لا يوجد commit/HEAD. التقريرين الأصليين في `Reports/` لم يتغيرا.

## نسخة قاعدة البيانات وسلامتها

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
| دورة إدارية 1→4→1 | SQL UPDATE قُبل، ثم رُجع ضمن test transaction | SQL يرفض، وAPI يعيد 400 دون تعديل |
| فلترة فترة متداخلة | الطلب 28/9–3/10 يختفي من فلتر أكتوبر | unit test يثبت ظهوره وحدود التقاطع |

## بوابات البناء والتشغيل

| الأمر | آخر نتيجة مؤكدة |
| --- | --- |
| `dotnet test RMS-BACKEND.slnx --no-restore --logger "console;verbosity=minimal"` | 82 Passed، 0 Failed، 0 Skipped؛ بعد الهجرة 008 |
| `npm --prefix FrontEnd test` | 4 Passed، 0 Failed |
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

لم يُجرَ اختبار كل إجراء في كل شاشة بعد. `RMS_Verification_Matrix.csv` يسرد المنفذ وغير المنفذ دون اعتبار عرض صفحة أو build دليلًا كافيًا. سياسة carryover التجارية غير محددة؛ سُئل مالك النظام عن الحد والانتهاء، وبقي `APP-BT-027` محجوبًا. لا توجد قياسات أداء ممثلة لـN+1 والفهارس، ولا اختبار إنشاء قاعدة جديدة أو restore drill بسبب قيد قاعدة `RMS` وحدها. لذلك هذا سجل تقدم لا شهادة اكتمال أو جاهزية إنتاج.
