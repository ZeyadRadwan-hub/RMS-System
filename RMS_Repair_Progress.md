# سجل تقدم إصلاح RMS

## تحديث 2026-10-05 — أحدث حالة (الأقسام الأقدم أدناه سجل تاريخي)

أحدث تحديث لبيانات الاختبار: بطلب المستخدم ضُبطت كلمات مرور الحسابات الخمسة على قيمة نصية واحدة جديدة بدون Password Hash. قبل التعديل أُخذت نسخة `RMS_20261005_pre_shared_password_refresh.bak` بـCOPY_ONLY/CHECKSUM واجتازت RESTORE VERIFYONLY. غُيّرت القيم الخمس داخل transaction واحدة وأُلغيت 91 جلسة نشطة، مع بقاء Employees=5 وTransactions=4 وMedicalDocuments=0 وRequestDecisionAudit=2. تحقق SQL من قيمة واحدة بطول 10 لكل الحسابات وصفر جلسات نشطة؛ لم يُجرَ اختبار دخول/واجهة جديد تنفيذًا لطلب المستخدم بعدم تشغيل اختبارات. عُدّل اختبار تخزين كلمات المرور ليتحقق من النطاق 9–128 بدل طول القيمة القديمة، لكنه لم يُشغّل بعد. APP-BT-007/008 وDB-BT-003 ما زالت Open لأن النص الصريح والكلمة المشتركة خطران معروفان.

قرار المستخدم بتاريخ 2026-10-05: تأجيل APP-BT-027 (Carryover) لمراجعة الفريق؛ لا يُعتبر مُصلحًا ولا يُفترض له سقف أو انتهاء أو أولوية خصم. تابع بقية البنود المستقلة. أمر shutdown ملغى نهائيًا بطلب المستخدم. آخر إضافة: browser_quick_insights.py أثبت إخفاء النافذة بالـCSS على touch قبل الإصلاح ثم اجتاز touch/Enter/Escape/ARIA بعد إزالة قاعدة الإخفاء؛ اختبار قارئ شاشة فعلي ما زال غير منفذ.

**أحدث إضافة بعد هذا العنوان:** `RMS_Repair_Report.md` و`RMS_Repair_Traceability.csv` يتتبعان 95 بندًا (87 أصلية + 8 جديدة). الحالة: 85 Fixed & Verified، 6 Fixed, Not Verified، 3 Open، 1 Blocked؛ لا إعلان اكتمال. `RMS_Verification_Matrix.csv` يلخص سيناريوهات التحقق، و`RMS_Database_Schema_Review.md` يلخص التغييرات البنيوية للفريق. آخر Backend كامل 114/114؛ Frontend 8/8 مع lint/build ناجحين **قبل أحدث تغيير لكلمة المرور**؛ browser_smoke.py اجتاز المسارات المذكورة قبل هذا التغيير. أحدث بيانات RMS: Employees=5 وTransactions=4 وMedicalDocuments=0 وRequestDecisionAudit=2، مع كلمة المرور التجريبية المشتركة في 5/5 حسابات. نُشرت التغييرات السابقة إلى GitHub `main` مع توثيق البنود المفتوحة؛ الرفع لا يعني اكتمال النظام. هذه الفقرة أحدث من الأرقام التاريخية أدناه.

تحديث التحقق الأحدث 2026-10-05: أعيد تشغيل Backend 114/114 وFrontend 8/8 وlint/build بنجاح. `browser_smoke.py` أعيد على Edge headless واتضح نجاح أدوار Employee/Manager/HR/Board ومسارات الطلبات والملف الطبي وCSV؛ التنظيف أبقى Employees=5 وTransactions=4 وMedicalDocuments=0 وRequestDecisionAudit=2. أُخذت نسخة إضافية `RMS_20261005_pre_hash_rotation.bak` بـCOPY_ONLY/CHECKSUM واجتازت RESTORE VERIFYONLY، لكن لم تُنفذ هجرة كلمات مرور بعدها. بعد توجيه المستخدم بعدم استخدام Hash لكلمات المرور، أُلغيت خطة إعادة Hash وحُذفت أداة `RMS-PasswordMigration` القديمة من المشروع. لا يزال قرار إبقاء كلمة موحدة أو جعلها مختلفة نصًا صريحًا غير محسوم؛ APP-BT-007/008 وDB-BT-003 تظل Open. Hash رموز الجلسات والملفات ليس Hash كلمات مرور ولم يُحذف. رُفعت نسخة العمل إلى GitHub مع بقاء العيوب المفتوحة موثقة.

تحديث المصادقة 2026-10-05: أوضح المستخدم أن تجاوز `Pass@1234` كان اختبارًا مؤقتًا وطلب حذفه، ثم اختار صراحة تخزين النص الصريح المشترك لجميع حسابات RMS التجريبية. أزيل التجاوز (NEW-007 Fixed & Verified)؛ طبّقنا سكربت 009 بعد نسخة `RMS_20261005_pre_plaintext_passwords.bak` اجتازت `RESTORE VERIFYONLY`، فحدث 5 حسابات وألغى 242 جلسة نشطة. `browser_shared_password.py` اجتاز دخول وخروج الحسابات الخمسة؛ `AuthorizationTests` أثبت 401 للقيمة القديمة و200 للكلمة المخزنة، وإنشاء موظف جديد/تغيير كلمة مروره نصًا وإلغاء جلسته. عادت المخاطر الأصلية APP-BT-007 وAPP-BT-008 وDB-BT-003 إلى Open بقرار المستخدم على نسخة الاختبار. browser_status_filters.py اجتاز فلاتر الحالة والتاريخ في خمس صفحات ببيانات mock، وأصلحنا تداخل قائمة History (NEW-008). المعاملة Id=881 وسجلا القرار الظاهران بعد خط الأساس السابق حُفظوا كما هم.

إضافة الجولة الحالية: DataAnnotations وcross-field validation للـDTOs، واختبارات حدود صحيحة/غير صالحة وHTTP 400 دون بيانات اختبار محفوظة. Backend الكامل 112/112. اجتازت browser_recovery_filters.py فلاتر Employees وحالات فشل HTTP 500/connectionfailed ثم recovery وRetry للرصيد. أُغلقت APP-BT-033/042/047/048 بالدليل؛ APP-BT-020 ما زال يحتاج بقية حدود الفلاتر وmultipart. أُغلق DB-BT-021 باختبار lookup_recovery.sql بعد نسخة متحققة؛ استعاد status/type مفقودين دون استبدال الموجود، ثم rollback ومقارنة كل صف أصلي نجحا. صُححت وثائق التشغيل والمصادقة، ولا يوجد إجراء shutdown إضافي بناءً على إلغاء المستخدم الصريح.

آخر بوابة قبول طازجة (2026-10-05): browser smoke نجح بكل المسارات بما فيها Board approval وHistory CSV وProfile؛ ثم أُعيد تشغيل `dotnet test` (85/85)، `npm test` (5/5)، lint (0 errors/0 warnings)، build، و`npm audit` (0 vulnerabilities). أُعيد تشغيل `database-setup.sql` على قاعدة RMS بعد نسخة `RMS_20261005_pre_setup_idempotence.bak` متحققة؛ بقيت أعداد Employees=5 وTransactions=3 وStatuses=11 وLevels=2 وTypes=5. أزيلت كلمات المرور التجريبية الثابتة من الوثائق واستُبدلت بإرشاد provisioning الآمن، وأضيف batch/pagination لمسارات الرصيد والطلبات.

العمل مستمر ولم يتحقق شرط الإغلاق الشامل. الاختبار الحالي على `(localdb)\MSSQLLocalDB`/`RMS` فقط؛ الحساب `DASH\Zeyad Radwan`. قبل كل هجرة أو تعديل بيانات جوهري أُخذت نسخة `COPY_ONLY, CHECKSUM` خارج OneDrive مع `RESTORE VERIFYONLY`. أحدث نسخة قبل هجرة سجل القرار: `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261005_pre_decision_audit.bak`. لا يوجد اختبار استعادة فعلي لأن المستخدم لم يصرح بقاعدة ثانية.

- المطبق: 002 جلسات المصادقة، 003 قيود السلامة، 004 تسلسلات المعرفات/منع التداخل، 005 المستندات الطبية، 006 حارس محاولات الدخول، 007 سجل هوية قرار الموافقة/الرفض. `RMS-BACKEND/Database/MIGRATIONS.md` يذكرها وتوجد سكربتات rollback منفصلة. البيانات القديمة غير المنسوبة في سجل القرار لم تُنسب زيفًا لأي شخص.
- كلمات مرور الحسابات الخمسة حُولت إلى hashes عشوائية منفصلة. ملف التسليم السري خارج OneDrive في `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/Provisioning/RMS_20261004_credentials.tsv` بصلاحيات Windows مقيدة؛ لا تطبع محتوياته. سكربت التأسيس لم يعد يحتوي حسابات/كلمات مرور ثابتة.
- اختبارات المتصفح الحية: دخول موظف وHR، مسارات الدور، إنشاء Annual، إنشاء Sick بمرفق multipart، قائمة/تنزيل المرفق، موافقة HR، تنقل الموبايل. السبب الجديد `NEW-002`: شاشات My Requests وHR Requests وLeave Balance استعملت خدمة JSON بدل multipart؛ صُححت. `RMS_Test_Evidence/browser_smoke.py` ينظف فقط الصفوف ذات marker دقيق. آخر تشغيل نجح.
- تعديل جديد: `DB-BT-015` أصبح له سجل `RequestDecisionAudit` مع actor/status/time/message داخل transaction. اختبار منفرد فشل بغياب الجدول ثم نجح بعد الهجرة.
- اختبار 50 إنشاء متوازيًا كشف 45 HTTP 500 قبل قفل SQL تطبيقي لكل موظف، ثم نجح 50/50 بعد الإصلاح. اختبار طلبين متداخلين متزامنين: واحد فقط حُفظ. اختبار قراري HR متسابقين أثبت قبول القرارين قبل الإصلاح، ثم نجح قرار واحد فقط بعد قفل المعاملة على مستوى SQL. هذه مشاكل جديدة يجب إضافتها إلى سجل `NEW-*` النهائي.
- `APP-BT-031`: اختبار Pending HR فشل 1 بدل 2، ثم أصلح DashboardService ونجح. `APP-BT-032`: فلترة فترة التداخل صارت helper مشتركة باختبار Node. `APP-BT-048`: Retry ظاهر عند فشل Leave Balance. أزيلت console logs التي تطبع قوائم الطلبات.
- أحدث نتيجة Backend كاملة: 79/79، صفر فشل، قبل إضافة اختبار التداخل الأخير؛ ينبغي إعادتها بعد آخر تعديل. أحدث `npm test`: 4/4. lint: صفر أخطاء و12 تحذير hooks؛ لا تزال التحذيرات مفتوحة. آخر browser smoke نجح بعد 007. فحص SQL بعد الاختبارات: Employees=5، Transactions=3، MedicalDocuments=0، RequestDecisionAudit=0، test-marker rows=0.
- الاعتماديات: `npm audit` بعد التحديث الأخير صفر advisories حسب التشغيل السابق؛ يلزم إعادة بوابة نهائية. `Microsoft.OpenApi` رُفع إلى 2.7.5 وأُزيل تحذير NU1903. CSV export يحمي من formula injection، وأزيلت حزمة `xlsx` القديمة.
- المفتوح المؤثر: سياسة carryover الدقيقة تحتاج قرار صاحب النظام؛ لا ينبغي اختراع سقف/مدة. كذلك يلزم استكمال تحقق UI لكل المسارات وCRUD الموظفين والأدوار والحالات، التحذيرات، أداء بيانات ممثلة، إعادة بناء قاعدة جديدة ضمن نطاق التصريح، التقرير العربي وسجلا CSV. لا إعلان اكتمال.

### المهمة التالية

اختبر وأصلح ما تبقى من دورات الموظفين/المدير والواجهة، حدّث التوثيق، ثم أنشئ سجل traceability لكل 87 صفًا + المشكلات الجديدة ومصفوفة تحقق/أدلة مع حالات صادقة. اسأل عن carryover بعد استنفاد العمل المستقل عنه.

**تاريخ البدء:** 2026-10-04 (Africa/Cairo)  
**الحالة:** قيد التنفيذ — لا يوجد إعلان اكتمال.  
**النطاق:** 66 بندًا في `Reports/Bug_Report.xlsx` و21 بندًا في `Reports/Bug_Report_Database.xlsx`؛ المجموع 87 صفًا قبل دمج الأسباب المشتركة.  
**المعرفات:** `APP-BT-001..066` و`DB-BT-001..021`، مع إبقاء كل صف أصلي في سجل التتبع النهائي.

## البيئة وخط الأساس

- مجلد المشروع: `C:/Users/workstation/OneDrive/Desktop/Software-Projects/RMS-Capstone/RMS/RMS/RMS`.
- لا توجد بيانات Git في هذا المجلد (`git rev-parse` يفشل)؛ لن يُستخدم reset/clean.
- .NET SDK 10.0.203؛ Node 22.17.0؛ npm 11.13.0؛ SQLCMD 16.0.1000.6.
- قاعدة الاختبار الوحيدة: `RMS` على `(localdb)\MSSQLLocalDB` بحساب Windows `DASH\Zeyad Radwan`، و`db_owner=1` وقت الفحص.
- الصفوف قبل الإصلاح: Employees 5، Transactions 3، Statuses 11، EmployeeLevels 2، TransactionTypes 5.
- النسخة الاحتياطية: `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261004_pre_repair.bak`، بحجم 3,985,408 bytes. `BACKUP ... WITH COPY_ONLY, CHECKSUM` نجح و`RESTORE VERIFYONLY ... WITH CHECKSUM` نجح. لم تُنفذ استعادة فعلية لأن قيد المستخدم يحصر الاختبارات في `RMS` ولا يسمح باستعمال قاعدة أخرى.
- نسخة ثانية قبل تدوير كلمات المرور: `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261004_pre_password_rotation.bak`؛ `COPY_ONLY/CHECKSUM` و`RESTORE VERIFYONLY` نجحا. ملف كلمات المرور الجديدة الوحيد في مجلد Provisioning خارج المشروع، ACL له فقط `DASH\Zeyad Radwan` و`SYSTEM`: `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/Provisioning/RMS_20261004_credentials.tsv`. لا تُنسخ محتوياته إلى التقرير.
- نسخة ثالثة قبل قيود السلامة: `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261004_pre_constraints.bak`؛ `COPY_ONLY/CHECKSUM` و`RESTORE VERIFYONLY` نجحا. هجرة `003_integrity_constraints.sql` أضافت 7 CHECKs و3 triggers بعد فحص مسبق بلا صفوف مخالفة؛ اختبار DB فشل 9/9 قبلها ثم نجح 14/14 بعدها، ومنها حالات إدخال صحيحة. بقيت صفوف Employees=5 وTransactions=3.
- نسخة رابعة قبل قيد التداخل وتسلسل المعرفات: `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261004_pre_request_integrity.bak`، اجتازت النسخ و`RESTORE VERIFYONLY`. هجرة `004_request_integrity_ids.sql` أضافت trigger منع تداخل الطلبات النشطة، فهرسين، وتسلسلين مستقلين لمعرفات الموظفين والطلبات. محاولة SQL الأولى فشلت بالصياغة قبل أي DDL (أكدنا 0 كائنات جديدة)، ثم أصلح السكربت ونُفذ بنجاح. آخر فحص: Employees=5، Transactions=3، سجلات الاختبار المتبقية=0.
- `dotnet build RMS-BACKEND.slnx --no-restore`: نجح (0 errors)، تحذير NU1903 في Microsoft.OpenApi 2.4.1.
- `dotnet test RMS-BACKEND.slnx --no-restore`: خروج 0 بلا اختبارات مكتشفة؛ لا يعد نجاحًا وظيفيًا.
- `npm run build`: نجح، bundle JS الرئيسي 1,034.75 kB، تحذير حجم chunk.
- `npm run lint`: فشل بـ10 أخطاء و11 تحذيرًا.
- لا تزال أخطاء Runtime القديمة بحاجة إلى إعادة اختبار بعد مواءمة الـschema.

## قرارات مثبتة

- قاعدة `RMS` الحالية هي مرجع التوافق الأولي لأنها تحتوي البيانات والعلاقات الفعلية. سنعدل EF لقراءتها بدل إعادة تسمية الجداول المأهولة لمجرد إرضاء الـModels القديمة.
- `ResponseMessage` في DB nullable (2 من 3 صفوف NULL)، لذا الطلب المعلق لا يحتاج رسالة رد قبل القرار.
- سيتم فصل الدليل الحي عن الاستنتاج من الكود؛ كل بند غير متحقق يظل Open أو Fixed, Not Verified.
- سياسة carryover، احتساب شهر التعيين، التعيين المستقبلي، حدود أيام الإجازة، وسياسة تحويل Manager role تحتاج مراجعة المستندات قبل تنفيذ حسابات مؤثرة.

## خطة التنفيذ

1. **Fixed & Verified مبدئيًا:** توافق EF مع قاعدة RMS وإزالة DDL الصامت من startup؛ اختبار `SchemaCompatibility` فشل أولًا بـ`Invalid object name 'EmployeeLevel'` ثم اجتاز (1/1)، وفحص `001_baseline_validation.sql` نجح. API smoke بعد التعديل: employees=5، transactions=3، leavebalance/2=200. يجب إعادة هذه البوابات في دورة القبول النهائية. الخطة التفصيلية في `docs/superpowers/plans/2026-10-04-rms-schema-compatibility.md`.
2. **قيد التنفيذ:** مصادقة وصلاحيات حقيقية، حماية البيانات وكلمات المرور، DTO validation، error handling. اختبار `AuthorizationTests` أثبت قبل الإصلاح أن `/employees` و`/transactions/all` و`/leavebalance/all` ترجع 200 مع headers مزوّرة؛ بعد إضافة جلسة Bearer وFallback Policy صارت 401. اختبار ترقية موظف بهيدر HR فشل سابقًا بـ200 ثم اجتاز بـ403. لا تُعد بقية مسارات الصلاحيات متحققة بعد. خطة المرحلة في `docs/superpowers/plans/2026-10-04-rms-authz.md`.
   - أُعيد التحقق من النسخة الاحتياطية بـ`RESTORE VERIFYONLY` قبل هجرة `002_auth_sessions.sql`. أضيف جدول `AuthSessions` على قاعدة RMS فقط، مع FK إلى Employees وقيود فريدة/انتهاء. بعدها بقيت أعداد الصفوف الأساسية 5/3/11/2/5 كما كانت، والجلسات بدأت من صفر. سكربت rollback منفصل يمنع حذف سجل جلسات غير فارغ.
   - أحدث build للواجهة: ناجح مع تحذير حجم bundle. تحذير Microsoft.OpenApi NU1903 لا يزال مفتوحًا.
   - التوسع الأخير: 35/35 اختبار Authorization ناجح (يشمل 23 مسارًا غير login، scopes، cross-team approve/reject، الانتهاء والإلغاء). اختبار `PasswordStorage` فشل أولًا لأن جميع السجلات الخمسة نصية ومتطابقة، ثم نجح 2/2 بعد تدوير كل حساب إلى سر عشوائي منفرد و`PasswordHasher`، مع بقاء Employees=5 وTransactions=3. HTTP حي بعد الهجرة: login الصحيح 200، `/auth/me` 200، كلمة مرور خاطئة 401، logout 204، الرمز الملغى 401.
   - `appsettings.json` صار يشير إلى `(localdb)\MSSQLLocalDB`/`RMS` بدل ProjectModels. لم تُختبر بعد مسارات تغيير كلمة المرور، ولا إزالة seed الثابت، ولا إغلاق كل تسريبات الاستثناء.
3. **Open:** هجرات قيود البيانات والمعرفات الآمنة، الحذف الناعم، سجل القرارات، وذرية المعاملات.
4. **Open:** سياسة الإجازات والأرصدة والطلبات والسباقات بعد حسم القواعد التجارية بالمصادر.
   - حساب Bonus، اقتطاع المعاملات العابرة للسنة/تاريخ asOf، ومنع accrual قبل نهاية 6 أشهر probation: 4 اختبارات فشلت أولًا ثم اجتازت بعد تعديل `LeaveBalanceService`. سياسة carryover الدقيقة ما زالت غير محددة في README؛ لا يُدعى إغلاق هذا البند.
   - تحقق الطلبات الحالي يرفض التاريخ المعكوس، نوع الإجازة غير المعروف، التداخل، والرصيد غير الكافي. اختبارات `TransactionValidation` فشلت في التداخل/الرصيد قبل الإصلاح ثم اجتازت 5/5، بما فيها إنشاء طلب صحيح وقراءته وتنظيف صف الاختبار. اختبار تخصيص ID فشل بوجود MAX+1 ثم اجتاز 2/2 بالـsequences. اختبار DB للتداخل فشل قبل trigger ثم اجتاز 16/16 بعده. يلزم اختبار تنافس متزامن وتفصيل سياسة الرصيد العابر للسنة.
5. **Open:** عقود API والواجهة، الملفات الطبية، التواريخ، التنقل، الخطأ/التحميل، التصدير، accessibility.
   - `CustomDatePicker` كان يحول تاريخ القاهرة المحلي إلى UTC فينقص يومًا؛ اختبار Node الأحمر 2026-10-04→2026-10-03 صار أخضر 1/1 باستخدام تاريخ محلي. عُدلت مسارات Board/Manager في الواجهة وبعض حقول/lookup الموظفين. الملفات الطبية لم تُنفذ بعد.
6. **Open:** الأداء والاعتماديات وlint واختبارات unit/API/browser والقبول النهائي.
7. **Open:** `RMS_Repair_Traceability.csv` بكل الـ87 صفًا، `RMS_Verification_Matrix.csv`، `RMS_Test_Evidence/`، و`RMS_Repair_Report.md` بالعربية.

## المهمة التالية

إكمال تدفق الملف الطبي والـseed وerror handling، ثم اختبارات التزامن والمتصفح وتقرير التتبع. تغير الـschema بإضافة AuthSessions والقيود والتسلسلات والفهارس، وأُنشئت صفوف جلسات اختبارية وسجلات تاريخها؛ تغيرت قيم `Employees.Password` إلى hashes منفصلة، مع بقاء IDs والصفوف والمعاملات. لا إعلان اكتمال بعد.
