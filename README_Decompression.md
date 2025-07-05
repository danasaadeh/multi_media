# خوارزميات فك الضغط - Compression Vault

## نظرة عامة

تم إضافة خوارزميات فك الضغط لـ **Shannon-Fano** و **Huffman** إلى مشروع Compression Vault. هذه الخوارزميات تدعم فك ضغط الملفات المضغوطة بنفس الطريقة التي تم بها الضغط، مع دعم تقسيم الملفات والمعالجة المتوازية.

## الملفات المضافة

### 1. واجهات الخدمات
- `Services/IDecompressionAlgorithm.cs` - الواجهة المشتركة لخوارزميات فك الضغط
- `Services/DecompressionService.cs` - خدمة فك الضغط الموحدة

### 2. خوارزميات فك الضغط
- `Algorithms/ShannonFanoDecompression.cs` - خوارزمية فك الضغط لـ Shannon-Fano
- `Algorithms/HuffmanDecompression.cs` - خوارزمية فك الضغط لـ Huffman

### 3. المعالجات المتوازية
- `Algorithms/ShannonFanoParallelDecompressor.cs` - معالج متوازي لفك الضغط Shannon-Fano
- `Algorithms/HuffmanParallelDecompressor.cs` - معالج متوازي لفك الضغط Huffman

### 4. النماذج والمديرين
- `Models/DecompressionModels.cs` - نماذج البيانات لفك الضغط
- `Managers/DecompressionManager.cs` - مدير فك الضغط

## الميزات الرئيسية

### 1. كشف تلقائي للخوارزمية
- يتم كشف نوع خوارزمية الضغط تلقائياً من رأس الملف
- دعم Magic Numbers:
  - `CVS1` - Shannon-Fano
  - `CVH1` - Huffman

### 2. دعم كلمات المرور
- التحقق من كلمات المرور باستخدام SHA256
- تفعيل/إلغاء تفعيل حقول كلمة المرور حسب الحاجة

### 3. المعالجة المتوازية
- فك ضغط متعدد الملفات بشكل متوازي
- استخدام SemaphoreSlim للتحكم في عدد العمليات المتزامنة
- إمكانية إلغاء العمليات

### 4. تتبع التقدم
- عرض نسبة التقدم في الوقت الفعلي
- إحصائيات مفصلة عن عملية فك الضغط
- رسائل حالة واضحة

## كيفية الاستخدام

### 1. فك الضغط الأساسي
```csharp
var decompressionManager = new DecompressionManager();
var info = new DecompressionInfo
{
    InputPath = "archive.cva",
    OutputDirectory = "C:\\Extracted",
    Password = "mypassword", // اختياري
    AutoDetectAlgorithm = true
};

var result = await decompressionManager.DecompressAsync(info);
```

### 2. فك الضغط مع تتبع التقدم
```csharp
var progress = new Progress<DecompressionProgress>(p => 
{
    Console.WriteLine($"Progress: {p.Percentage:F1}% - {p.Status}");
});

var result = await decompressionManager.DecompressAsync(info, progress);
```

### 3. فك الضغط متعدد الملفات
```csharp
var infos = new List<DecompressionInfo>
{
    new DecompressionInfo { InputPath = "archive1.cva", OutputDirectory = "C:\\Extract1" },
    new DecompressionInfo { InputPath = "archive2.cva", OutputDirectory = "C:\\Extract2" }
};

var results = await decompressionManager.DecompressMultipleAsync(infos);
```

## بنية الملف المضغوط

### رأس الملف
```
Magic Number (4 bytes): "CVS1" أو "CVH1"
Password Flag (1 byte): true/false
Password Hash (if enabled): length + hash bytes
Item Count (4 bytes): عدد العناصر
Item Metadata: لكل عنصر
  - Name (string)
  - Size (8 bytes)
  - File Count (4 bytes)
  - Is Folder (1 byte)
```

### بيانات العنصر
```
File Name (string)
Original Size (8 bytes)
Compressed Size (8 bytes)
Frequency Table Count (4 bytes)
Frequency Table (if compressed):
  - Key (1 byte)
  - Value (4 bytes)
Compressed Data (bytes)
Valid Bits in Last Byte (1 byte)
```

## خوارزمية فك الضغط

### Shannon-Fano Decompression
1. قراءة رأس الملف والتحقق من Magic Number
2. التحقق من كلمة المرور إذا كانت مطلوبة
3. قراءة جدول الترددات
4. بناء شجرة Shannon-Fano
5. فك ضغط البيانات باستخدام الشجرة
6. كتابة البيانات المفكوكة

### Huffman Decompression
1. قراءة رأس الملف والتحقق من Magic Number
2. التحقق من كلمة المرور إذا كانت مطلوبة
3. قراءة جدول الترددات
4. بناء شجرة Huffman
5. فك ضغط البيانات باستخدام الشجرة
6. كتابة البيانات المفكوكة

## إدارة الأخطاء

### أنواع الأخطاء المدعومة
- ملف غير موجود
- تنسيق ملف غير صحيح
- كلمة مرور خاطئة
- ملف تالف
- أخطاء في القراءة/الكتابة

### معالجة الأخطاء
```csharp
try
{
    var result = await decompressionManager.DecompressAsync(info);
    if (result.Success)
    {
        Console.WriteLine($"Extracted {result.ExtractedFiles.Count} files");
    }
    else
    {
        Console.WriteLine($"Error: {result.ErrorMessage}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## واجهة المستخدم

تم إضافة تبويب فك الضغط إلى النموذج الرئيسي مع:
- اختيار ملف الأرشيف
- اختيار مجلد الاستخراج
- إدخال كلمة المرور
- عرض معلومات الأرشيف
- شريط التقدم
- إحصائيات فك الضغط

## الأداء والتحسينات

### المعالجة المتوازية
- استخدام Task.Run للعمليات الثقيلة
- SemaphoreSlim للتحكم في التزامن
- إمكانية إلغاء العمليات

### إدارة الذاكرة
- قراءة البيانات على دفعات
- إطلاق الموارد تلقائياً
- استخدام using statements

### التحسينات المستقبلية
- دعم ضغط متعدد المستويات
- خوارزميات ضغط إضافية
- واجهة سطر أوامر
- دعم الشبكات

## الاختبار

### اختبار الوظائف الأساسية
```csharp
// اختبار فك الضغط
var testFile = "test_archive.cva";
var testOutput = "test_output";
var result = await decompressionManager.DecompressAsync(new DecompressionInfo
{
    InputPath = testFile,
    OutputDirectory = testOutput
});
Assert.IsTrue(result.Success);
```

### اختبار الأخطاء
```csharp
// اختبار ملف غير موجود
var result = await decompressionManager.DecompressAsync(new DecompressionInfo
{
    InputPath = "nonexistent.cva",
    OutputDirectory = "output"
});
Assert.IsFalse(result.Success);
Assert.IsTrue(result.ErrorMessage.Contains("does not exist"));
```

## الخلاصة

تم بنجاح إضافة خوارزميات فك الضغط الكاملة لـ Shannon-Fano و Huffman إلى مشروع Compression Vault. هذه الخوارزميات تدعم جميع ميزات الضغط الأصلية مع إضافة تحسينات في الأداء وسهولة الاستخدام. 