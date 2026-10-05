# سجل تقدم إصلاح RMS

## تحديث 2026-10-05 — أحدث حالة (الأقسام الأقدم أدناه سجل تاريخي)

**أحدث إضافة بعد هذا العنوان:** أُنجزت ملفات `RMS_Repair_Report.md` (93 بندًا فرديًا)، `RMS_Repair_Traceability.csv` (87 أصلية + 6 جديدة، بلا ID مكرر أو صف مصدر ناقص)، و`RMS_Verification_Matrix.csv` (28 سيناريو/شاشة)، ودليل `RMS_Test_Evidence/acceptance_2026-10-05.md`. حالتها الحالية 76 Fixed & Verified، 13 Fixed, Not Verified، 3 Open، 1 Blocked؛ لذا **لا إعلان اكتمال**. أحدث Browser workflow يشمل create/edit/transfer/archive للموظف وقرار المدير ثم HR، مع تنظيف الصفوف إلى 5/3 الأصلية. أحدث Backend 82/82، Frontend 4/4، lint صفر تحذيرات، build بلا تحذير حجم بعد lazy loading (main ~309KB)، npm audit صفر. هجرة 008 لمنع دورات المدير متعددة المستويات طُبقت بعد backup متحقق. هذه الفقرة أحدث من الأرقام التاريخية أدناه.

آخر بوابة قبول طازجة (2026-10-05): browser smoke نجح بكل المسارات بما فيها Board approval وHistory CSV وProfile؛ ثم أُعيد تشغيل `dotnet test` (82/82)، `npm test` (4/4)، lint (0 errors/0 warnings)، build، و`npm audit` (0 vulnerabilities). أُعيد تشغيل `database-setup.sql` على قاعدة RMS بعد نسخة `RMS_20261005_pre_setup_idempotence.bak` متحققة؛ بقيت أعداد Employees=5 وTransactions=3 وStatuses=11 وLevels=2 وTypes=5. أزيلت كلمات المرور التجريبية الثابتة من الوثائق واستُبدلت بإرشاد provisioning الآمن.

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
