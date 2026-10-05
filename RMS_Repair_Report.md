# تقرير إصلاح RMS — 2026-10-05

الحالة: **العمل لم يكتمل بعد، ولا توجد شهادة جاهزية إنتاج.** أصلحنا واختبرنا أجزاء جوهرية، لكن بقيت بنود مفتوحة، وأخرى غُيّرت ولم تكتمل تغطيتها، وقاعدة carryover تحتاج قرار صاحب النظام. ملف CSV المرافق جزء من هذا التقرير ويحفظ كل صف أصلي ورقم الورقة/الصف وأولويته ودليل الإغلاق.

السجل: 93 بندًا = 87 أصلية + 6 جديدة. Fixed & Verified: 76؛ Fixed, Not Verified: 13؛ Open: 3؛ Blocked: 1. لا تُدمج الملاحظات المتكررة في الأعداد؛ لأنها صفوف أصلية يجب تتبعها منفردة.

## البيئة وحماية البيانات

التشغيل والاختبار على `(localdb)\MSSQLLocalDB` / `RMS` بحساب Windows `DASH\Zeyad Radwan` فقط. المجلد ليس Git repository. أخذنا نسخ `COPY_ONLY, CHECKSUM` خارج OneDrive قبل كل تغيير schema/بيانات مهم وتأكدنا بـ`RESTORE VERIFYONLY`؛ لم نجرِ restore فعليًا لأن التصريح لا يسمح بقاعدة أخرى. أعداد البيانات الأساسية بعد تنظيف الاختبارات: 5 موظفين، 3 معاملات، 0 مستند تجريبي، 0 سجل قرار تجريبي.

## نتيجة الاختبارات الحالية

- Backend: 82/82 اختبارًا ناجحًا بعد هجرة 008، وصفر skipped.
- Frontend Node: 4/4 ناجحة. `npm run lint`: صفر أخطاء/تحذيرات. `npm run build`: ناجح، ملف JS الأساسي نحو 309KB بلا تحذير الحجم. `npm audit --audit-level=low`: صفر ثغرات معلنة.
- Browser E2E ضد الواجهة والـAPI وSQL الحقيقية: دخول الموظف والمدير وHR، إنشاء Annual وSick ومرفق وتنزيله، إنشاء/تعديل/نقل/أرشفة موظف، موافقة المدير ثم HR، موبايل وكيبورد. تفاصيل الأوامر والنتائج في `RMS_Test_Evidence/acceptance_2026-10-05.md`.
- اختبارات التزامن: 50 طلبًا مستقلاً نجحت بعد فشل 45/50 قبل الإصلاح؛ طلبان متداخلان لا ينجح منهما إلا واحد؛ قراران نهائيان متسابقان لا ينجح منهما إلا واحد.

## الحدود والقرارات المطلوبة

- `APP-BT-027`: المستندات تذكر carryover من غير حد أقصى أو موعد انتهاء؛ لا يمكن وضع حساب رصيد مالي/إداري بالتخمين. طُلب تحديد السياسة.
- `APP-BT-049` و`DB-BT-014`: لم توجد بيانات حجم ممثل أو قياسات logical reads تثبت إغلاق N+1/fetch غير المحدود، رغم إضافة فهارس.
- `APP-BT-024` و`APP-BT-044`: سياسة خفض دور المدير القديم واشتراط المدير في كل قسم ما زالت غير محسومة/غير مثبتة كاملة. حماية self/cycle موجودة.
- صفوف Fixed, Not Verified تحتاج اختبارات مسارات بعينها قبل إغلاقها؛ راجع `RMS_Verification_Matrix.csv`. History وHR Requests وProfile وبعض الرفض/التعديل/الفلاتر لم تُختبر كاملة من المتصفح.
- إنشاء قاعدة جديدة وrestore drill لم يُنفذا تحت تصريح RMS-only؛ `RESTORE VERIFYONLY` ليس بديلًا عنهما. لا يجوز توسيع التصريح ضمنيًا.

## تطبيق الهجرات والرجوع

الهجرات 002–008 مطبقة على RMS التجريبية فقط، وتفاصيل كل خطوة وسكربت rollback في `RMS-BACKEND/Database/MIGRATIONS.md`. أي قاعدة أخرى تحتاج backup والتحقق من baseline قبل تطبيقها بالترتيب؛ لا يُشغل DDL من startup. كلمات المرور القديمة دُوّرت إلى hashes مستقلة، وملف provisioning سري خارج المشروع بصلاحيات Windows مقيدة؛ لا تظهر أسراره هنا. استعادة بيانات ما قبل التدوير تتطلب قرارًا منفصلًا لأنها تعيد أيضًا كلمة المرور الضعيفة القديمة.

## البنود الفردية

### APP-BT-001 — API بلا Authentication فعلي

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 8؛ الخطورة: Critical.
- السبب: لا يوجد هوية موثقة أو policy على controllers
- قبل/بعد: قبل: بالكود: لا AddAuthentication ولا UseAuthentication ولا [Authorize]؛ UseAuthorization وحده لا يحمي endpoints | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: جلسات Bearer موثقة وصلاحيات تعتمد actor من الخادم، مع عزل الموظف والفريق وHR وBoard وCORS محدد.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**.

### APP-BT-002 — انتحال هوية موظف عبر X-Employee-Id

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 9؛ الخطورة: Critical.
- السبب: هوية العميل مصدرها header قابل للتغيير
- قبل/بعد: قبل: بالكود: controller يمرر قيمة header مباشرة إلى service؛ يمكن إنشاء طلب باسم الرقم المرسل | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: جلسات Bearer موثقة وصلاحيات تعتمد actor من الخادم، مع عزل الموظف والفريق وHR وBoard وCORS محدد.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-003 — المدير يستطيع اعتماد طلب من خارج فريقه

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 10؛ الخطورة: Critical.
- السبب: شرط علاقة المدير بصاحب الطلب مفقود
- قبل/بعد: قبل: بالكود: CanApprove/CanReject يرجع true لأي Manager مع status=Pending؛ managerId وrequestOwnerId غير مستخدمين | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: جلسات Bearer موثقة وصلاحيات تعتمد actor من الخادم، مع عزل الموظف والفريق وHR وBoard وCORS محدد.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-004 — إدارة الموظفين العامة بلا قيد HR

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 11؛ الخطورة: Critical.
- السبب: وصف HR only تعليق فقط
- قبل/بعد: قبل: بالكود: لا [Authorize] ولا تحقق HR داخل actions | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: جلسات Bearer موثقة وصلاحيات تعتمد actor من الخادم، مع عزل الموظف والفريق وHR وBoard وCORS محدد.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-005 — كشف الطلبات والأرصدة لكل المؤسسة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 12؛ الخطورة: Critical.
- السبب: غياب فحص الملكية والدور على عمليات القراءة
- قبل/بعد: قبل: بالكود: GetAllRequests يتجاهل role؛ GetTransactionById لا يفحص ملكية؛ كل balance endpoints بلا auth | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: جلسات Bearer موثقة وصلاحيات تعتمد actor من الخادم، مع عزل الموظف والفريق وHR وBoard وCORS محدد.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-006 — ترقية دور التقارير عبر X-Employee-Role

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 13؛ الخطورة: Critical.
- السبب: الدور من header غير موثق مع fallback مفتوح
- قبل/بعد: قبل: بالكود: BuildBaseQuery لا يطبق filter إلا على Employee/Manager، وأي قيمة أخرى ترى الكل | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: جلسات Bearer موثقة وصلاحيات تعتمد actor من الخادم، مع عزل الموظف والفريق وHR وBoard وCORS محدد.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-007 — كلمات المرور محفوظة ومقارنة كنص صريح

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 14؛ الخطورة: Critical.
- السبب: لا يوجد hashing ولا password verifier
- قبل/بعد: قبل: بالكود: SQL predicate e.Password == password والحقل نص مباشر | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تدوير كلمات المرور إلى hashes فردية، provisioner بلا أسرار ثابتة، وحد محاولات موزع حسب الحساب والـIP.
- التحقق: PasswordStorage وAuthorizationTests؛ migrations 002/006. الحالة: **Fixed & Verified**.

### APP-BT-008 — حسابات تجريبية بكلمات مرور ثابتة في سكربت التأسيس

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 15؛ الخطورة: Critical.
- السبب: بيانات اعتماد افتراضية مضمنة في المصدر
- قبل/بعد: قبل: بالكود: seed يضيف حسابات بكلمة مرور ثابتة ويطبع بيانات الدخول | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تدوير كلمات المرور إلى hashes فردية، provisioner بلا أسرار ثابتة، وحد محاولات موزع حسب الحساب والـIP.
- التحقق: PasswordStorage وAuthorizationTests؛ migrations 002/006. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-007.

### APP-BT-009 — سكريبت إنشاء DB لا يطابق EF schema

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 16؛ الخطورة: Critical.
- السبب: مصدران مختلفان للـschema بلا migration متطابقة
- قبل/بعد: قبل: بالكود: السكربت ينشئ EmployeeLevels/Statuses وAnnualLeaveEntitlement/TransactionTypeName؛ النماذج تستخدم EmployeeLevel/Status وRegularLeaveperYear/Name | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: EF يطابق جداول RMS، ولا يوجد DDL صامت في startup؛ هجرات صريحة فقط.
- التحقق: SchemaCompatibility؛ 001_baseline_validation.sql. الحالة: **Fixed & Verified**.

### APP-BT-010 — أنواع الإجازة ترسل IDs خاطئة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 17؛ الخطورة: Critical.
- السبب: IDs hardcoded ومتعارضة مع seed
- قبل/بعد: قبل: بالكود: Casual يرسل 1=سِك في seed؛ Sick يرسل 3=Half Day | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: توحيد lookup IDs، رفع مستندات مرضية multipart وتخزينها، وإرسال الحقول المطلوبة عند إنشاء الموظف.
- التحقق: TransactionValidationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-009.

### APP-BT-011 — الملفات الطبية لا تُرفع أو تُخزن

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 18؛ الخطورة: Critical.
- السبب: ميزة upload غير موصولة بتخزين/DTO
- قبل/بعد: قبل: بالكود: File objects تُرسل في JSON وCreateTransactionRequestDto لا يحتوي ملفات؛ لا upload endpoint | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: توحيد lookup IDs، رفع مستندات مرضية multipart وتخزينها، وإرسال الحقول المطلوبة عند إنشاء الموظف.
- التحقق: TransactionValidationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-010.

### APP-BT-012 — إنشاء موظف لا يرسل كلمة مرور أو دور

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 19؛ الخطورة: Critical.
- السبب: عقد الواجهة والخادم غير متطابق
- قبل/بعد: قبل: بالكود: payload بلا password/employeeRole؛ DTO يملأهما بقيم افتراضية فارغة/0 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: توحيد lookup IDs، رفع مستندات مرضية multipart وتخزينها، وإرسال الحقول المطلوبة عند إنشاء الموظف.
- التحقق: TransactionValidationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-004.

### APP-BT-013 — التاريخ المختار ينقص يومًا في توقيت القاهرة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 20؛ الخطورة: Critical.
- السبب: تحويل تاريخ محلي إلى UTC قبل أخذ YYYY-MM-DD
- قبل/بعد: قبل: اختبار Node: local midnight ثم toISOString ينتج 2026-09-23 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تاريخ محلي بلا انزياح القاهرة، Sign صحيح لـBonus، والتحقق من التاريخ والنوع والتداخل والرصيد.
- التحقق: dateOnly.test.js؛ LeaveBalanceTests؛ TransactionValidationTests. الحالة: **Fixed & Verified**.

### APP-BT-014 — رصيد Bonus يُخصم بدل أن يُضاف

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 21؛ الخطورة: Critical.
- السبب: فقدان دلالة Sign بسبب Abs
- قبل/بعد: قبل: بالكود: Math.Abs(days) ثم طرحه؛ Sick ذات Sign=0 تبقى محايدة، لكن Bonus تُخصم | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تاريخ محلي بلا انزياح القاهرة، Sign صحيح لـBonus، والتحقق من التاريخ والنوع والتداخل والرصيد.
- التحقق: dateOnly.test.js؛ LeaveBalanceTests؛ TransactionValidationTests. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-009.

### APP-BT-015 — الطلبات لا تُراجع رصيدها أو صلاحية تواريخها في الخادم

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 22؛ الخطورة: Critical.
- السبب: التحقق موجود جزئيًا في الواجهة فقط
- قبل/بعد: قبل: بالكود: الحقول تُنسخ إلى Transaction دون فحص مدى التاريخ أو الرصيد أو التداخل | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تاريخ محلي بلا انزياح القاهرة، Sign صحيح لـBonus، والتحقق من التاريخ والنوع والتداخل والرصيد.
- التحقق: dateOnly.test.js؛ LeaveBalanceTests؛ TransactionValidationTests. الحالة: **Fixed & Verified**.

### APP-BT-016 — Token الصادر من login لا يُتحقق منه ولا تنتهي صلاحيته

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 26؛ الخطورة: Medium.
- السبب: غياب session/auth pipeline
- قبل/بعد: قبل: بالكود: token مجرد Base64 للـID والوقت؛ client لا يرسله أصلًا | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: جلسات Bearer موثقة وصلاحيات تعتمد actor من الخادم، مع عزل الموظف والفريق وHR وBoard وCORS محدد.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-017 — Login دون rate limit أو lockout

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 27؛ الخطورة: Medium.
- السبب: غياب حماية brute force
- قبل/بعد: قبل: بالكود: لا RateLimiter أو عداد محاولات | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تدوير كلمات المرور إلى hashes فردية، provisioner بلا أسرار ثابتة، وحد محاولات موزع حسب الحساب والـIP.
- التحقق: PasswordStorage وAuthorizationTests؛ migrations 002/006. الحالة: **Fixed & Verified**.

### APP-BT-018 — CORS يسمح بأي Origin وMethod وHeader

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 28؛ الخطورة: Medium.
- السبب: سياسة واسعة جدًا
- قبل/بعد: قبل: بالكود: AllowAnyOrigin/Method/Header | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: جلسات Bearer موثقة وصلاحيات تعتمد actor من الخادم، مع عزل الموظف والفريق وHR وBoard وCORS محدد.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-019 — الاستثناءات الداخلية ترجع للمستخدم

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 29؛ الخطورة: Medium.
- السبب: لا error middleware يفصل التفاصيل
- قبل/بعد: قبل: بالكود: catch يعيد ex.Message إلى HTTP response | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: أخطاء API مصنفة ورسائل SQL الداخلية محجوبة.
- التحقق: ApiError.cs؛ AuthorizationTests وTransactionValidationTests. الحالة: **Fixed & Verified**.

### APP-BT-020 — مدخلات DTO بلا validation كافٍ

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 30؛ الخطورة: Medium.
- السبب: الاعتماد على قيود DB والواجهة
- قبل/بعد: قبل: بالكود: DTOs بلا Required/Range/StringLength؛ enum cast لا يتحقق | بعد: الإغلاق غير مثبت بعد
- التعديل: أضيف تحقق إلى طلبات الموظف والطلبات والملفات، لكن لم يكتمل حصر جميع DTOs وحدودها.
- التحقق: اختبارات validation الحالية؛ تغطية DTO شاملة لم تُنفذ. الحالة: **Fixed, Not Verified**.

### APP-BT-021 — حذف الموظف Hard Delete خلاف التوثيق

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 31؛ الخطورة: Medium.
- السبب: التنفيذ لا يستخدم حالة الحذف الموثقة
- قبل/بعد: قبل: بالكود: _context.Employees.Remove؛ لا IsDeleted في model | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: حذف ناعم، sequences للمعرفات، وعمليات موظف ذرية مع تحديث المدير.
- التحقق: EmployeeRepository؛ IdentifierAllocationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**.

### APP-BT-022 — توليد ID عبر MAX+1 يسبب تصادمات متزامنة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 32؛ الخطورة: Medium.
- السبب: توليد ID خارج DB دون sequence/identity
- قبل/بعد: قبل: بالكود: كلاهما قد يقرأ نفس max ثم يكتب نفس ID | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: حذف ناعم، sequences للمعرفات، وعمليات موظف ذرية مع تحديث المدير.
- التحقق: EmployeeRepository؛ IdentifierAllocationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**.

### APP-BT-023 — إنشاء موظف وتحديث المدير غير ذريين

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 33؛ الخطورة: Medium.
- السبب: لا DB transaction وابتلاع استثناء التحديث
- قبل/بعد: قبل: بالكود: employee يُحفظ أولًا ثم تحديث المدير في try/catch يبتلع الخطأ | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: حذف ناعم، sequences للمعرفات، وعمليات موظف ذرية مع تحديث المدير.
- التحقق: EmployeeRepository؛ IdentifierAllocationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**.

### APP-BT-024 — تغيير/حذف تابع يترك Manager role قديمًا

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 34؛ الخطورة: Medium.
- السبب: مسار إزالة التابع غير معالج
- قبل/بعد: قبل: بالكود: UpdateManagerStatus يستدعى للمدير الجديد فقط ويرفع role ولا يخفضه | بعد: الإغلاق غير مثبت بعد
- التعديل: منع خفض مدير له تابعون فُعّل؛ سياسة خفض المدير القديم بعد النقل لم تُحسم/تُختبر بالكامل.
- التحقق: EmployeesController؛ مطلوب قرار السياسة واختبار نقل متعدد. الحالة: **Open**؛ الاعتماديات: BT-003.

### APP-BT-025 — التطبيق يغير schema عند كل startup ويخفي الفشل

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 35؛ الخطورة: Medium.
- السبب: DDL غير مُدار ومخفي
- قبل/بعد: قبل: بالكود: ExecuteSqlRaw DROP FK/ALTER COLUMN داخل startup وcatch فارغ | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: EF يطابق جداول RMS، ولا يوجد DDL صامت في startup؛ هجرات صريحة فقط.
- التحقق: SchemaCompatibility؛ 001_baseline_validation.sql. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-009.

### APP-BT-026 — فترة التجربة لا تمنع accrual فعليًا

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 36؛ الخطورة: Medium.
- السبب: حساب السنة يتجاهل probation وتاريخ الالتحاق
- قبل/بعد: قبل: بالكود: IsInProbation=true لكن earnedDays=9×annual/12 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إصلاح probation والقص حسب السنة وasOf والشهور المؤهلة واحتساب Pending HR.
- التحقق: LeaveBalanceTests؛ Pending_stats_include_requests_awaiting_HR. الحالة: **Fixed & Verified**.

### APP-BT-027 — Carryover موثق لكنه دائمًا صفر

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 37؛ الخطورة: Medium.
- السبب: الميزة غير منفذة
- قبل/بعد: قبل: بالكود: CarryoverFromPreviousYear = 0 ثابت | بعد: الإغلاق غير مثبت بعد
- التعديل: لم تُنفذ معادلة carryover لأن حد الترحيل والانتهاء غير محددين في المستندات؛ طُلب قرار مالك النظام.
- التحقق: README.md؛ سؤال سياسة carryover. الحالة: **Blocked**.

### APP-BT-028 — إجازة عابرة للسنة لا تدخل رصيد السنة الجديدة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 38؛ الخطورة: Medium.
- السبب: نطاق query على تاريخ البداية فقط
- قبل/بعد: قبل: بالكود: query يشترط StartDate.Year == currentYear فيستبعد الطلب | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إصلاح probation والقص حسب السنة وasOf والشهور المؤهلة واحتساب Pending HR.
- التحقق: LeaveBalanceTests؛ Pending_stats_include_requests_awaiting_HR. الحالة: **Fixed & Verified**.

### APP-BT-029 — طلب يبدأ قبل asOf وينتهي بعده يُخصم كاملًا

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 39؛ الخطورة: Medium.
- السبب: لا قص للفترة عند asOf
- قبل/بعد: قبل: بالكود: شرط البداية فقط ثم حساب المدة كلها | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إصلاح probation والقص حسب السنة وasOf والشهور المؤهلة واحتساب Pending HR.
- التحقق: LeaveBalanceTests؛ Pending_stats_include_requests_awaiting_HR. الحالة: **Fixed & Verified**.

### APP-BT-030 — accrual يحسب شهورًا قبل توظيف الموظف

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 40؛ الخطورة: Medium.
- السبب: استخدام رقم الشهر الحالي بدل شهور العمل المؤهلة
- قبل/بعد: قبل: بالكود: earnedDays = 9×monthlyAccrual | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إصلاح probation والقص حسب السنة وasOf والشهور المؤهلة واحتساب Pending HR.
- التحقق: LeaveBalanceTests؛ Pending_stats_include_requests_awaiting_HR. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-026.

### APP-BT-031 — Dashboard لا يحسب Pending HR ضمن Pending

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 41؛ الخطورة: Medium.
- السبب: تصنيف حالة ناقص
- قبل/بعد: قبل: بالكود: PendingRequests يعد status 1 فقط | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إصلاح probation والقص حسب السنة وasOf والشهور المؤهلة واحتساب Pending HR.
- التحقق: LeaveBalanceTests؛ Pending_stats_include_requests_awaiting_HR. الحالة: **Fixed & Verified**.

### APP-BT-032 — فلترة التاريخ تستبعد الطلبات المتداخلة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 42؛ الخطورة: Medium.
- السبب: استعمال containment بدل overlap
- قبل/بعد: قبل: بالكود: start>=from وend<=to، فيختفي | بعد: الإغلاق غير مثبت بعد
- التعديل: فلترة الفترة أصبحت overlap مشتركة بدل containment في الصفحات الرئيسية.
- التحقق: dateRange.test.js؛ فلاتر كل شاشة لم تُجرب بالمتصفح. الحالة: **Fixed, Not Verified**.

### APP-BT-033 — Fallback API URL والوثائق غير متوافقين مع launch settings

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 43؛ الخطورة: Medium.
- السبب: إعدادات مشتتة والـfallback قديم
- قبل/بعد: قبل: fallback=HTTPS 5001 غير مضبوط؛ .env الحالي يستخدم HTTP 5190 ويعمل من حيث الـport | بعد: الإغلاق غير مثبت بعد
- التعديل: تكوين API التطوير والإنتاج أصبح صريحًا؛ توثيق التشغيل التفصيلي ما زال بحاجة مراجعة نهائية.
- التحقق: FrontEnd/src/services/api.js؛ build؛ HOW_TO_RUN.md. الحالة: **Fixed, Not Verified**.

### APP-BT-034 — reject يرسل reason بدل responseMessage

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 44؛ الخطورة: Medium.
- السبب: اختلاف أسماء DTO
- قبل/بعد: قبل: بالكود: body={reason} ويُربط ResponseMessage افتراضيًا فارغًا | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تصحيح عقود approve/reject والحذف الوهمي، وحماية المسارات والتخزين المحلي من انتحال الدور.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**.

### APP-BT-035 — approve في requestService يرسل POST بلا body

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 45؛ الخطورة: Medium.
- السبب: نسختا service مختلفتان
- قبل/بعد: قبل: بالكود: axios.post(url) دون body بينما action يتطلب [FromBody] | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تصحيح عقود approve/reject والحذف الوهمي، وحماية المسارات والتخزين المحلي من انتحال الدور.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**.

### APP-BT-036 — requestService.delete يستدعي endpoint غير موجود

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 46؛ الخطورة: Medium.
- السبب: service قديمة عن API
- قبل/بعد: قبل: بالكود: لا [HttpDelete] للـtransactions | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تصحيح عقود approve/reject والحذف الوهمي، وحماية المسارات والتخزين المحلي من انتحال الدور.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**.

### APP-BT-037 — Team Requests route متاح لكل logged-in user

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 47؛ الخطورة: Medium.
- السبب: الـSidebar يخفي الرابط فقط
- قبل/بعد: قبل: بالكود: ProtectedRoute بلا requiredRole | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تصحيح عقود approve/reject والحذف الوهمي، وحماية المسارات والتخزين المحلي من انتحال الدور.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-038 — Board يستطيع دخول My Requests من الرابط

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 48؛ الخطورة: Medium.
- السبب: منع العرض في القائمة فقط
- قبل/بعد: قبل: بالكود: المسار غير مغلف بـ BlockBoardRoute | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تصحيح عقود approve/reject والحذف الوهمي، وحماية المسارات والتخزين المحلي من انتحال الدور.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-039 — صلاحيات الواجهة تثق ببيانات localStorage

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 49؛ الخطورة: Medium.
- السبب: هوية مخزنة دون تحقق
- قبل/بعد: قبل: بالكود: isHR/isBoard مبنيان على user المحلي | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تصحيح عقود approve/reject والحذف الوهمي، وحماية المسارات والتخزين المحلي من انتحال الدور.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### APP-BT-040 — JSON محلي تالف قد يكسر بدء التطبيق والطلبات

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 50؛ الخطورة: Medium.
- السبب: عدم تحمل بيانات جلسة فاسدة
- قبل/بعد: قبل: بالكود: JSON.parse بلا try/catch | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تصحيح عقود approve/reject والحذف الوهمي، وحماية المسارات والتخزين المحلي من انتحال الدور.
- التحقق: AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**.

### APP-BT-041 — تعديل موظف لا يملأ manager وlevel

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 51؛ الخطورة: Medium.
- السبب: عقد response لا يوفر level ID والتسمية مختلفة
- قبل/بعد: قبل: بالكود: form يقرأ managerID وemployeeLevelID؛ response فيه managerId وEmployeeLevel نص فقط | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تعبئة manager/level عند التعديل ومطابقة مستوى B/الأقسام مع lookup والجدول.
- التحقق: EmployeeFormModal.jsx؛ browser_employee_create_edit_transfer_archive. الحالة: **Fixed & Verified**.

### APP-BT-042 — فلاتر الموظفين تستخدم حقولًا وأقسامًا غير صحيحة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 52؛ الخطورة: Medium.
- السبب: hardcoded filters وعقد DTO قديم
- قبل/بعد: قبل: بالكود: role/levelID/levelName غير موجودة في DTO؛ IDs الأقسام 1-4 لا 7-11 | بعد: الإغلاق غير مثبت بعد
- التعديل: حقول فلترة الموظفين صارت تطابق DTO؛ اختبار الفلاتر بالمتصفح لم يكتمل.
- التحقق: Employees.jsx؛ browser smoke يغطي الجدول لا كل الفلاتر. الحالة: **Fixed, Not Verified**؛ الاعتماديات: BT-009.

### APP-BT-043 — قيمة Level B وأسماء الأقسام متعارضة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 53؛ الخطورة: Medium.
- السبب: lookup hardcoded ومصادر غير متسقة
- قبل/بعد: قبل: بالكود: الواجهة تقول B=20 وMarketing/Finance؛ seed يقول B=24 وProduction/IT | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تعبئة manager/level عند التعديل ومطابقة مستوى B/الأقسام مع lookup والجدول.
- التحقق: EmployeeFormModal.jsx؛ browser_employee_create_edit_transfer_archive. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-009.

### APP-BT-044 — المدير مطلوب ويمكن اختيار الموظف نفسه

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 54؛ الخطورة: Medium.
- السبب: قواعد hierarchy ناقصة
- قبل/بعد: قبل: بالكود: form يلزم managerID وlist قد تشمل الموظف الحالي؛ server لا يتحقق من cycle | بعد: الإغلاق غير مثبت بعد
- التعديل: الواجهة ما زالت تشترط مديرًا لغير HR رغم سماح العقد ببعض الحالات؛ منع self/cycle موجود في الخادم والقاعدة.
- التحقق: EmployeeFormModal.jsx؛ 008_manager_hierarchy.sql. الحالة: **Open**.

### APP-BT-045 — قائمة التنقل مختفية على الموبايل بلا زر فتح

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 55؛ الخطورة: Medium.
- السبب: state collapsed لا يطابق CSS mobile
- قبل/بعد: قبل: بالكود: .sidebar خارج الشاشة وتعود فقط مع .open؛ component لا يضيف .open | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: زر قائمة للموبايل وClear All يعيد فلتر الحالة أيضًا.
- التحقق: browser_smoke.py؛ Dashboard.jsx. الحالة: **Fixed & Verified**.

### APP-BT-046 — Dashboard Clear All يبقي فلتر Approved

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 56؛ الخطورة: Medium.
- السبب: default filter مستخدم عند reset
- قبل/بعد: قبل: بالكود: statusID يعاد إلى 3؛ التوزيع الافتراضي Approved فقط | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: زر قائمة للموبايل وClear All يعيد فلتر الحالة أيضًا.
- التحقق: browser_smoke.py؛ Dashboard.jsx. الحالة: **Fixed & Verified**.

### APP-BT-047 — فشل أرصدة الموظفين يتحول إلى أصفار صامتة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 57؛ الخطورة: Medium.
- السبب: failure swallowed
- قبل/بعد: قبل: بالكود: catch يتجاهل الفشل ثم detail modal يستخدم قيمًا افتراضية | بعد: الإغلاق غير مثبت بعد
- التعديل: خطأ الرصيد لم يعد صفرًا صامتًا وظهرت Retry في Leave Balance؛ اختبار فشل الشبكة ثم Retry غير مكتمل.
- التحقق: Employees.jsx؛ LeaveBalance.jsx. الحالة: **Fixed, Not Verified**.

### APP-BT-048 — فشل تحميل صفحة Leave Balance لا يترك Retry

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 58؛ الخطورة: Medium.
- السبب: غياب error state للصفحة
- قبل/بعد: قبل: بالكود: showAlert فقط وتستمر الصفحة دون balance | بعد: الإغلاق غير مثبت بعد
- التعديل: خطأ الرصيد لم يعد صفرًا صامتًا وظهرت Retry في Leave Balance؛ اختبار فشل الشبكة ثم Retry غير مكتمل.
- التحقق: Employees.jsx؛ LeaveBalance.jsx. الحالة: **Fixed, Not Verified**.

### APP-BT-049 — N+1 وجلب غير محدود للأرصدة والطلبات

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 59؛ الخطورة: Medium.
- السبب: لا batch aggregation ولا pagination
- قبل/بعد: قبل: بالكود: balance ينفذ query لكل موظف، وقوائم transactions ToList كاملة | بعد: الإغلاق غير مثبت بعد
- التعديل: لم تُثبت معالجة N+1 أو الجلب غير المحدود بقياسات على حجم ممثل.
- التحقق: مطلوب خطة قياس/فهارس وpaging. الحالة: **Open**.

### APP-BT-050 — حزم إنتاجية عليها advisories مؤكدة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 60؛ الخطورة: Medium.
- السبب: إصدارات مثبتة متأثرة؛ قابلية التطبيق لكل advisory تحتاج تقييم
- قبل/بعد: قبل: npm audit: 6 packages (5 high,1 moderate)، وdotnet: NU1903 Microsoft.OpenApi 2.4.1 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: تحديث الحزم المعرضة وإزالة xlsx القديم؛ npm audit بلا advisories.
- التحقق: npm audit --audit-level=low: 0. الحالة: **Fixed & Verified**.

### APP-BT-051 — لا توجد اختبارات project-owned للـFront/Back

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 61؛ الخطورة: Medium.
- السبب: لا test suites مضمنة
- قبل/بعد: قبل: dotnet test خرج 0 بلا اختبارات؛ package.json بلا test script؛ rg لا يجد test files | بعد: الإغلاق غير مثبت بعد
- التعديل: أضيفت اختبارات Backend وFrontend وbrowser حقيقية، لكن تغطية كل المسارات والحالات لم تكتمل.
- التحقق: 82 Backend؛ 4 Node؛ browser_smoke.py. الحالة: **Fixed, Not Verified**.

### APP-BT-052 — ESLint يفشل بـ21 مشكلة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 62؛ الخطورة: Medium.
- السبب: متغيرات غير مستخدمة وhooks dependencies/react-refresh
- قبل/بعد: قبل: exit 1: 10 errors و11 warnings | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إزالة أخطاء وتحذيرات ESLint الناتجة عن imports وتأثيرات React.
- التحقق: npm run lint: 0 errors, 0 warnings. الحالة: **Fixed & Verified**.

### APP-BT-053 — لا تحقق نوع/حجم ملف طبي قبل القبول

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 63؛ الخطورة: Medium.
- السبب: القيود نصوص UI فقط
- قبل/بعد: قبل: بالكود: addFiles يقبل كل File؛ accept لا ينطبق على drag/drop | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: فحص نوع/حجم المرفقات وإعداد API/timeout واضحين.
- التحقق: MedicalFileValidator؛ TransactionValidationTests؛ build. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-011.

### APP-BT-054 — غياب timeout وتهيئة production API واضحة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 64؛ الخطورة: Medium.
- السبب: تهيئة transport ناقصة
- قبل/بعد: قبل: بالكود: axios.create بلا timeout؛ fallback localhost جهاز المستخدم | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: فحص نوع/حجم المرفقات وإعداد API/timeout واضحين.
- التحقق: MedicalFileValidator؛ TransactionValidationTests؛ build. الحالة: **Fixed & Verified**.

### APP-BT-055 — التعامل مع الحالات يعتمد على نص statusName

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 68؛ الخطورة: Low.
- السبب: ربط business logic بعرض النص
- قبل/بعد: قبل: بالكود: المقارنات على النصوص في عدة صفحات | بعد: الإغلاق غير مثبت بعد
- التعديل: قرارات العرض الرئيسية تستخدم statusID بدل النص؛ لم تُفحص كل فلاتر/وسوم الحالة يدويًا.
- التحقق: AllRequests/TeamRequests/History؛ browser_smoke.py. الحالة: **Fixed, Not Verified**.

### APP-BT-056 — History search لا يبحث بكود الموظف

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 69؛ الخطورة: Low.
- السبب: label أوسع من التنفيذ
- قبل/بعد: قبل: بالكود: filter يقرأ employeeName فقط | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: بحث History بكود الموظف وتصدير CSV بحالات موحدة وحماية من formula injection.
- التحقق: exportCsv.test.js. الحالة: **Fixed & Verified**.

### APP-BT-057 — History export يعرض حالة خام مختلفة عن الجدول

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 70؛ الخطورة: Low.
- السبب: مسار عرض ومسار تصدير مختلفان
- قبل/بعد: قبل: بالكود: export يستخدم req.statusName مباشرة | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: بحث History بكود الموظف وتصدير CSV بحالات موحدة وحماية من formula injection.
- التحقق: exportCsv.test.js. الحالة: **Fixed & Verified**.

### APP-BT-058 — تسمية 45-Day Cap غير مستندة إلى Backend

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 71؛ الخطورة: Low.
- السبب: حساب توضيحي غير موثوق
- قبل/بعد: قبل: بالكود: الواجهة تستنتج cap والـservice يقول لا hardcoded caps | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إزالة تسمية سقف 45 يومًا غير المثبتة وإخفاء Team tab عن غير المدير.
- التحقق: LeaveBalance.jsx؛ browser role navigation. الحالة: **Fixed & Verified**.

### APP-BT-059 — Team tab يظهر لموظف غير مدير

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 72؛ الخطورة: Low.
- السبب: شرط الظهور أوسع من الصلاحية
- قبل/بعد: قبل: بالكود: showTeamTab = !isBoard، فيراه Employee | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إزالة تسمية سقف 45 يومًا غير المثبتة وإخفاء Team tab عن غير المدير.
- التحقق: LeaveBalance.jsx؛ browser role navigation. الحالة: **Fixed & Verified**.

### APP-BT-060 — Custom controls وmodals ناقصة keyboard/ARIA

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 73؛ الخطورة: Low.
- السبب: عدم تطبيق semantics للـcontrols
- قبل/بعد: قبل: بالكود: triggers/options من div وmodals بلا dialog/focus trap | بعد: الإغلاق غير مثبت بعد
- التعديل: CustomSelect وDatePicker قابلان للكيبورد مع ARIA أولي؛ إدارة focus للـmodals لم تُستكمل.
- التحقق: browser_smoke.py keyboard_select. الحالة: **Fixed, Not Verified**.

### APP-BT-061 — Quick Insights يعتمد على hover ويختفي على اللمس

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 74؛ الخطورة: Low.
- السبب: تفاعل أحادي modality
- قبل/بعد: قبل: بالكود: CSS يخفي popover على touch ولا tap بديل | بعد: الإغلاق غير مثبت بعد
- التعديل: Quick Insights أصبح قابلًا للنقر واللمس، لكن لم يكتمل اختبار اللمس/قارئ الشاشة.
- التحقق: QuickInsightsPopover.jsx. الحالة: **Fixed, Not Verified**.

### APP-BT-062 — Console logs تطبع بيانات الموظفين والطلبات

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 75؛ الخطورة: Low.
- السبب: debug logging متروك
- قبل/بعد: قبل: بالكود: console.log يطبع payload كاملًا | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إزالة console.log الذي كان يطبع قوائم الموظفين والطلبات.
- التحقق: rg console.log FrontEnd/src: لا نتائج. الحالة: **Fixed & Verified**.

### APP-BT-063 — حزم ومكونات غير مستخدمة تكبر الصيانة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 76؛ الخطورة: Low.
- السبب: تراكم تنفيذات قديمة
- قبل/بعد: قبل: بالكود: ag-grid/ag-charts وملفات خدمة/واجهة لا imports لها؛ حجم JS المبني >1MB لسبب يحتاج تحليلًا منفصلًا | بعد: الإغلاق غير مثبت بعد
- التعديل: إزالة حزم Grid/Charts غير المستخدمة وتقسيم تحميل الصفحات؛ مراجعة المكونات المهجورة لم تكتمل.
- التحقق: npm uninstall؛ build: main 308.61KB. الحالة: **Fixed, Not Verified**.

### APP-BT-064 — Contact Administrator رابط وهمي

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 77؛ الخطورة: Low.
- السبب: placeholder UI
- قبل/بعد: قبل: بالكود: href="#" | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إزالة رابط الإدارة الوهمي، مزامنة شهر منتقي التاريخ، وإظهار خطأ إلغاء الطلب.
- التحقق: Login.jsx؛ CustomDatePicker.jsx؛ MyRequests.jsx. الحالة: **Fixed & Verified**.

### APP-BT-065 — الـDate Picker لا يزامن شهر العرض مع value الجديدة

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 78؛ الخطورة: Low.
- السبب: state مشتقة قديمة
- قبل/بعد: قبل: بالكود: viewDate يبدأ من value مرة واحدة ولا effect للمزامنة | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إزالة رابط الإدارة الوهمي، مزامنة شهر منتقي التاريخ، وإظهار خطأ إلغاء الطلب.
- التحقق: Login.jsx؛ CustomDatePicker.jsx؛ MyRequests.jsx. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-013.

### APP-BT-066 — نتيجة فشل Cancel لا تظهر رسالة Backend

- المصدر: Bug_Report.xlsx، ورقة Bug Report، صف 79؛ الخطورة: Low.
- السبب: فقدان response.data.message
- قبل/بعد: قبل: بالكود: العرض يستخدم error.message فقط | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: إزالة رابط الإدارة الوهمي، مزامنة شهر منتقي التاريخ، وإظهار خطأ إلغاء الطلب.
- التحقق: Login.jsx؛ CustomDatePicker.jsx؛ MyRequests.jsx. الحالة: **Fixed & Verified**.

### DB-BT-001 — أسماء جدولي المستوى والحالة لا تطابق EF

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 8؛ الخطورة: Critical.
- السبب: EF يطلب EmployeeLevel وStatus مفردين بينما RMS تحتوي EmployeeLevels وStatuses
- قبل/بعد: قبل: HTTP 400/500: Invalid object name EmployeeLevel أو Status | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: مواءمة EF مع schema الحي وإزالة ALTER عند التشغيل؛ ResponseMessage nullable.
- التحقق: SchemaCompatibility؛ baseline validation. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-002.

### DB-BT-002 — أعمدة EF غير موجودة في قاعدة RMS

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 9؛ الخطورة: Critical.
- السبب: نموذج EF وسكربت DB يمثلان عقدين مختلفين
- قبل/بعد: قبل: EmployeeLevels تملك LevelName/AnnualLeaveEntitlement بدل LevelDescription/RegularLeaveperYear/CasualLeavePerYear/OrderId؛ Statuses تملك StatusType بدل Entity/OrderNumber؛ TransactionTypes تملك TransactionTypeName ولا Description | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: مواءمة EF مع schema الحي وإزالة ALTER عند التشغيل؛ ResponseMessage nullable.
- التحقق: SchemaCompatibility؛ baseline validation. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### DB-BT-003 — كلمات المرور نص صريح وجميع الحسابات التجريبية تشترك في قيمة واحدة

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 10؛ الخطورة: Critical.
- السبب: تصميم كلمة المرور والـseed بلا hashing أو إجبار تعيين سر مختلف
- قبل/بعد: قبل: 5 موظفين وCOUNT(DISTINCT Password)=1؛ repository يقارن Password مباشرة بالنص المدخل | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: كلمات مرور hash فردية وقيد يمنع الفارغة.
- التحقق: PasswordStorage؛ migration 003. الحالة: **Fixed & Verified**.

### DB-BT-004 — قاعدة البيانات تقبل تاريخ نهاية قبل البداية

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 11؛ الخطورة: Critical.
- السبب: لا CHECK (EndDate >= StartDate) ولا تحقق كافٍ بالخدمة
- قبل/بعد: قبل: INSERT نجح ثم ROLLBACK؛ عدد Transactions عاد 3 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: CHECKs وtriggers للتواريخ والدور والمدير والنطاق وSign/Unit وتاريخ التوظيف المستقبلي وفق السياسة الحالية.
- التحقق: DatabaseConstraintTests؛ migrations 003/008. الحالة: **Fixed & Verified**.

### DB-BT-005 — الحالة والقسم يشتركان في جدول واحد دون قيد النوع

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 12؛ الخطورة: Critical.
- السبب: FKs على Statuses بلا قيد StatusType أو فصل lookup domains
- قبل/بعد: قبل: العمليتان نجحتا ثم ROLLBACK؛ FKs تتحقق من ID فقط | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: CHECKs وtriggers للتواريخ والدور والمدير والنطاق وSign/Unit وتاريخ التوظيف المستقبلي وفق السياسة الحالية.
- التحقق: DatabaseConstraintTests؛ migrations 003/008. الحالة: **Fixed & Verified**.

### DB-BT-006 — وحدة ونوع إشارة الإجازة تقبل قيمًا غير صالحة

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 13؛ الخطورة: Critical.
- السبب: لا CHECK للـUnit أو Sign
- قبل/بعد: قبل: UPDATE نجح ثم ROLLBACK وعادت Unit=1, Sign=-1 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: CHECKs وtriggers للتواريخ والدور والمدير والنطاق وSign/Unit وتاريخ التوظيف المستقبلي وفق السياسة الحالية.
- التحقق: DatabaseConstraintTests؛ migrations 003/008. الحالة: **Fixed & Verified**.

### DB-BT-007 — HTTP يعرض أسماء كائنات SQL ورسائل الاستثناء

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 14؛ الخطورة: Critical.
- السبب: إرجاع ex.Message للعميل
- قبل/بعد: قبل: HTTP 400/500 يعرض Invalid object name EmployeeLevel أو Status حرفيًا | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: منع إخراج استثناءات SQL التفصيلية عبر HTTP.
- التحقق: ApiError.cs؛ TransactionValidationTests. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### DB-BT-008 — طلبان متداخلان/مكرران لنفس الموظف مقبولان

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 18؛ الخطورة: Medium.
- السبب: لا قيد أو فحص تداخل داخل معاملة ذرية
- قبل/بعد: قبل: صفان متماثلان لنفس employee/type/dates أثناء transaction؛ ROLLBACK أعاد العدد 3 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: trigger تداخل SQL وتسلسلات معرفات؛ قفل transaction على SQL يمنع سباق الإنشاء والقرار.
- التحقق: 50 concurrent؛ overlap race؛ migrations 004. الحالة: **Fixed & Verified**.

### DB-BT-009 — المدير يمكن جعله مدير نفسه ودوره خارج enum

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 19؛ الخطورة: Medium.
- السبب: FK ذاتي لا يمنع ID=ID؛ لا CHECK للدور
- قبل/بعد: قبل: UPDATE نجح ثم ROLLBACK؛ رجع ManagerId=1 وRole=0 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: CHECKs وtriggers للتواريخ والدور والمدير والنطاق وSign/Unit وتاريخ التوظيف المستقبلي وفق السياسة الحالية.
- التحقق: DatabaseConstraintTests؛ migrations 003/008. الحالة: **Fixed & Verified**.

### DB-BT-010 — قاعدة البيانات تسمح بكلمة مرور فارغة

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 20؛ الخطورة: Medium.
- السبب: NOT NULL لا يمنع السلسلة الفارغة؛ DTO/الخادم بلا تحقق كافٍ
- قبل/بعد: قبل: UPDATE نجح ثم ROLLBACK؛ القيمة الأصلية غير فارغة | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: كلمات مرور hash فردية وقيد يمنع الفارغة.
- التحقق: PasswordStorage؛ migration 003. الحالة: **Fixed & Verified**.

### DB-BT-011 — حذف فعلي رغم وجود IsDeleted

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 21؛ الخطورة: Medium.
- السبب: النموذج ومسار الحذف لا يستخدمان العمود الموجود
- قبل/بعد: قبل: الصف اختفى فعليًا أثناء transaction ثم عاد بعد ROLLBACK؛ repository يستخدم Remove ولا يمثل IsDeleted | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: حذف ناعم يبقي السجل وتمنع الجلسة للحساب المؤرشف.
- التحقق: EmployeeRepository؛ AuthorizationTests؛ browser_smoke.py. الحالة: **Fixed & Verified**.

### DB-BT-012 — توليد المعرفات بطريقة MAX+1 غير آمن للتزامن

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 22؛ الخطورة: Medium.
- السبب: المعرّف يولد بالتطبيق خارج sequence/identity
- قبل/بعد: قبل: الكود يحسب MAX+1؛ PK سيمنع أحد الإدخالين عند السباق | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: trigger تداخل SQL وتسلسلات معرفات؛ قفل transaction على SQL يمنع سباق الإنشاء والقرار.
- التحقق: 50 concurrent؛ overlap race؛ migrations 004. الحالة: **Fixed & Verified**.

### DB-BT-013 — تعديل schema في بدء الخادم مع catch صامت

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 23؛ الخطورة: Medium.
- السبب: إصلاح آني غير مُدار بإصدار ولا يسجل الفشل
- قبل/بعد: قبل: startup نفذ ALTER TABLE SubstituteEmployeeId INT NULL؛ العمود كان nullable؛ اسم FK في الكود لا يطابق اسم FK الفعلي؛ لا جدول migration history؛ catch فارغ | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: مواءمة EF مع schema الحي وإزالة ALTER عند التشغيل؛ ResponseMessage nullable.
- التحقق: SchemaCompatibility؛ baseline validation. الحالة: **Fixed & Verified**.

### DB-BT-014 — لا توجد فهارس على مفاتيح معاملات الربط والفلترة

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 24؛ الخطورة: Medium.
- السبب: الـFK لا ينشئ index تلقائيًا في SQL Server
- قبل/بعد: قبل: Transactions بها PK فقط؛ لا index ثانوي على EmployeeId/StatusID/TransactionTypesID | بعد: الإغلاق غير مثبت بعد
- التعديل: فهرسان للطلبات وفهرس المستندات أُضيفوا؛ قياس logical reads على حجم ممثل لم يكتمل.
- التحقق: migration 004/005؛ لا benchmark ممثل. الحالة: **Fixed, Not Verified**.

### DB-BT-015 — لا يوجد توثيق بنيوي لهوية صاحب قرار الموافقة

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 25؛ الخطورة: Medium.
- السبب: نموذج المعاملة يختزل آخر حالة بلا audit history
- قبل/بعد: قبل: الجدول يحفظ ResponseDate/ResponseMessage فقط ولا يحفظ ApproverId أو انتقالات الحالات | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: جدول RequestDecisionAudit يحفظ actor والحالة قبل/بعد والتوقيت والرسالة داخل transaction.
- التحقق: Hr_approval_records_actor؛ competing decisions race؛ migration 007. الحالة: **Fixed & Verified**.

### DB-BT-016 — قيم أنواع الإجازة بالواجهة لا تطابق lookup الحية

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 26؛ الخطورة: Medium.
- السبب: IDs hardcoded بدل lookup API
- قبل/بعد: قبل: DB: 1=Sick Leave و3=Half Day؛ الواجهة تثبت Sick=3 وCasual=1 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: lookup IDs وSign/Bonus ومستوى B مطابقون للواجهة والحساب.
- التحقق: browser_smoke.py؛ LeaveBalanceTests. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### DB-BT-017 — حساب الرصيد يفقد إشارة Bonus

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 27؛ الخطورة: Medium.
- السبب: حساب الأيام يزيل علامة النوع
- قبل/بعد: قبل: الكود يستخدم Math.Abs(days) ثم يطرح؛ DB تميز Bonus بـSign=+1 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: lookup IDs وSign/Bonus ومستوى B مطابقون للواجهة والحساب.
- التحقق: browser_smoke.py؛ LeaveBalanceTests. الحالة: **Fixed & Verified**؛ الاعتماديات: BT-001.

### DB-BT-018 — استحقاق Level B والتقسيمات لا تطابق قيم الواجهة الثابتة

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 28؛ الخطورة: Medium.
- السبب: قيم reference data منسوخة ومثبتة
- قبل/بعد: قبل: DB: B=24 يوم؛ الواجهة تعرض 20. DB departments Quality/Production/IT/HR/Board؛ خيارات الواجهة مختلفة | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: lookup IDs وSign/Bonus ومستوى B مطابقون للواجهة والحساب.
- التحقق: browser_smoke.py؛ LeaveBalanceTests. الحالة: **Fixed & Verified**.

### DB-BT-019 — ResponseMessage فارغ في DB رغم Required بالنموذج

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 29؛ الخطورة: Medium.
- السبب: nullability بين schema/EF والسلوك غير متطابقة
- قبل/بعد: قبل: 2 من أصل 3 معاملات بها NULL؛ property معلّمة Required | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: مواءمة EF مع schema الحي وإزالة ALTER عند التشغيل؛ ResponseMessage nullable.
- التحقق: SchemaCompatibility؛ baseline validation. الحالة: **Fixed & Verified**.

### DB-BT-020 — DateOfEmployment يقبل تاريخًا مستقبليًا

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 30؛ الخطورة: Medium.
- السبب: لا validation/قيد مرتبط بسياسة تاريخ الالتحاق
- قبل/بعد: قبل: UPDATE نجح ثم ROLLBACK واستعيد 2021-06-15 | بعد: السلوك المحدد مثبت بالدليل المذكور
- التعديل: CHECKs وtriggers للتواريخ والدور والمدير والنطاق وSign/Unit وتاريخ التوظيف المستقبلي وفق السياسة الحالية.
- التحقق: DatabaseConstraintTests؛ migrations 003/008. الحالة: **Fixed & Verified**.

### DB-BT-021 — سكريبت التأسيس غير مضمون عند إعادة تشغيل جزئية

- المصدر: Bug_Report_Database.xlsx، ورقة Bug Report، صف 34؛ الخطورة: Low.
- السبب: شرط idempotency على أول عنصر لا كل صف
- قبل/بعد: قبل: بعض blocks تفحص Id=1 فقط ثم تدرج مجموعة IDs؛ لو Id=1 موجود والآخر مفقود لا يضاف | بعد: الإغلاق غير مثبت بعد
- التعديل: seed أصبح idempotent على البيانات المأهولة؛ اختبار استرجاع lookup مفقودة في قاعدة جديدة غير مخول.
- التحقق: database-setup.sql re-run بلا تغييرات؛ fresh DB لم يُختبر. الحالة: **Fixed, Not Verified**.

### NEW-001 — تعارض EF OUTPUT مع triggers

- المصدر: فحص الإصلاح الحالي، اكتُشف أثناء الإصلاح؛ الخطورة: Medium.
- السبب: سلوك EF الافتراضي حاول استخدام OUTPUT على جداول لها triggers.
- قبل/بعد: قبل: فشل مثبت باختبار أحمر | بعد: اجتاز الاختبار نفسه
- التعديل: تعطيل SQL OUTPUT في خرائط الجداول المتأثرة.
- التحقق: SchemaCompatibility + TransactionValidationTests. الحالة: **Fixed & Verified**.

### NEW-002 — ضياع المرفق المرضي من 3 شاشات

- المصدر: فحص الإصلاح الحالي، اكتُشف أثناء الإصلاح؛ الخطورة: Critical.
- السبب: الشاشات استخدمت خدمة JSON بدل خدمة multipart.
- قبل/بعد: قبل: فشل مثبت باختبار أحمر | بعد: اجتاز الاختبار نفسه
- التعديل: توجيه create/update إلى requestService.
- التحقق: browser_sick_upload and document_list_download. الحالة: **Fixed & Verified**.

### NEW-003 — طلبات مستقلة متزامنة ترجع HTTP 500

- المصدر: فحص الإصلاح الحالي، اكتُشف أثناء الإصلاح؛ الخطورة: Critical.
- السبب: trigger التداخل قفل صفوفًا بعد INSERTs متوازية لنفس الموظف.
- قبل/بعد: قبل: فشل مثبت باختبار أحمر | بعد: اجتاز الاختبار نفسه
- التعديل: قفل SQL داخل transaction على الموظف قبل التحقق والإنشاء.
- التحقق: Fifty_independent_concurrent_requests_receive_unique_ids: 50/50. الحالة: **Fixed & Verified**.

### NEW-004 — قرارا موافقة ورفض نهائيان ينجحان معًا

- المصدر: فحص الإصلاح الحالي، اكتُشف أثناء الإصلاح؛ الخطورة: Critical.
- السبب: قُرئت الحالة قبل قفل المعاملة وتسلسل القرار.
- قبل/بعد: قبل: فشل مثبت باختبار أحمر | بعد: اجتاز الاختبار نفسه
- التعديل: قفل الطلب قبل القراءة والقرار مع حفظ سجل التدقيق ذريًا.
- التحقق: Competing_final_decisions_record_exactly_one_winner. الحالة: **Fixed & Verified**.

### NEW-005 — دورات إدارية متعددة المستويات مقبولة

- المصدر: فحص الإصلاح الحالي، اكتُشف أثناء الإصلاح؛ الخطورة: Medium.
- السبب: CHECK السابق منع المدير لنفسه فقط.
- قبل/بعد: قبل: فشل مثبت باختبار أحمر | بعد: اجتاز الاختبار نفسه
- التعديل: هجرة 008 تضيف trigger تكراريًا وتحقيقًا في API.
- التحقق: DatabaseConstraintTests + Manager_cycle_is_a_validation_error. الحالة: **Fixed & Verified**.

### NEW-006 — مستوى الموظف يظهر N/A

- المصدر: فحص الإصلاح الحالي، اكتُشف أثناء الإصلاح؛ الخطورة: Low.
- السبب: الجدول قرأ levelName بينما DTO يعرض employeeLevel.
- قبل/بعد: قبل: فشل مثبت باختبار أحمر | بعد: اجتاز الاختبار نفسه
- التعديل: استخدام اسم حقل DTO الصحيح.
- التحقق: browser_employee_create_edit_transfer_archive. الحالة: **Fixed & Verified**.
