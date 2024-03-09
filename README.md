# Decoy Traffic Maker
### NOTE: For personal use and R&D purposes only. <b> Use at your own risk </b>

این برنامه در فواصل زمانی مختلف و بصورت تصادفی، از طریق ارسال درخواست های متعدد و یا آپلود به مقاصد مختلف ترافیکی را روی شبکه شما ایجاد میکند تا تشخیص فغالیت سرور وی پی ان و روش های تانل برای سرویس دهنده ها سخت تر شود.

هدف از ایجاد این ترافیک گمراه کننده:
- Send / Recieve بر هم زدن نمودار مانیتورینگ ترافیک
- Decoy / گمراه کردن ترافیک سرور برای سانسورچی
-  افزایش میزان ترافیک آپلود به نسبت دانلود



## Installation - طریقه نصب /  پیش نیاز اجرای برنامه

* ابتدا .NET Runtime 8.0 را نصب کنید
  - Win/Mac: - Install .Net Runtime 8.0:
    - https://dotnet.microsoft.com/en-us/download/dotnet/8.0
  - Linux: ``` apt update && apt install dotnet-runtime-8.0 ```
    - اگر مخزن شما شامل دات نت نبود دات نت ۸ را به صورت دستی یا با اسکریپت رسمی مایکروسافت نصب کنید:
  - https://learn.microsoft.com/en-us/dotnet/core/install/linux
*  اخرین نسخه Release برنامه را از بخش release ها و یا با دستورات زیر در سرور خود دریافت کنید .

    * نسخه پرتابل / Portable کراس پلتفرم و قابل اجرا در هر سیستم عاملی است.
    * نسخه Self Contained کراس پلتفرم و بدون نیاز به نصب دات نت است ( به زودی)
    * نسخه linux64 مختص سیستم عامل لینوکس است
* فایل FakeTrafficMaker.App.exe یا FakeTrafficMaker.App.dll را اجرا کنید

#### You Can See and download all releases and other platform from here:
https://github.com/goombooli/DecoyTrafficMaker/releases/tag/latest/

### جهت دانلود و اجرای برنامه روی سرور خود دستورات زیر را دنبال کنید:
```bash
# download one of releases with wget or curl:
  wget https://github.com/goombooli/DecoyTrafficMaker/releases/download/latest/TrafficMaker_v0.6-beta1_Portable.zip # Cross Platform (Mac/Win/Linux)
# or with curl:
  curl -L https://github.com/goombooli/DecoyTrafficMaker/releases/download/latest/TrafficMaker_v0.6-beta1_Portable.zip
# extract release archive and cd to directory
  unzip TrafficMaker_v0.6-beta1_Portable.zip -d TrafficMaker
  cd TrafficMaker
# run program using dotnet cli (linux)
  dotnet FakeTrafficMaker.App.dll
# OR FakeTrafficMaker.App.exe for Windows

## LinuxOnly Version: https://github.com/goombooli/DecoyTrafficMaker/releases/download/latest/TrafficMaker_v0.6-beta1_linux64.zip #(Linux x64 Version Only)
```
اگر میخواهید در صورت بسته شدن ترمینال  ssh شما برنامه در پس زمنیه کار کند:
```bash
 apt install tmux
 tmux
```
 حال برنامه را در ترمینال tmux مانند دتسورات قبل اجرا کنید. در اینصورت برنامه با خروج از SSH و سرور در پس زمینه در حال اجرا خواهد ماند (اما در صورت ریستارت سرور باید مجدد اجرا گردد.)

 در صورتی که میخواهید در هر بار راه اندازی سرور برنامه اجرا شود مانند زیر crontab را با ویرایشگر متن خود باز کنید (در ویندوز یک شورتکات از فایل اجرایی برنامه را در start up قرار دهید):
 ```bash
 crontab -e
 # این خط را به انتهای فایل کران جاب ها اضافه کنید:
 @reboot dotnet /home/goombooli/TrafficMaker/FakeTrafficMaker.App.dll
 ```

## تنظیمات پیشرفته
در صورتی که میخواهید پارامتر های برنامه را تغییر دهید میتوانید فایل appsettings.json را با ادیتور خود ویرایش کنید:

پارامتر های مهم:
- Concurrency: تعداد عملیات همزمان در هر اجرا (پیشفرض 2)
- MinDataSize: حداقل سایز بافر برای آپلود ها
- Multiplier: ضریب سایز بافر آپلود جهت تصادفی سازی ترافیک
- ActivationTimes: استراحت و تاخیر بین هر آپلود  // secondly = 0, minutely = 1 / hourly = 2 / daily = 3
- FakeActivationTimesMultipler: آرایه و مضرب مدت زمان های تصادفی جهت ایجاد وقفه در هر عملیات
- DelayMinutes: حداقل وقفه هر اجرا
- DefaultClientAgent: یوزر ایجنت درخواست ها بصورت پیشفرض Mozila Windows NT
- DefaultClientTimeout: تایم اوت ریکوئست ها بصورت پیشفرض یک دقیقه

- همچنین میتوانید برنامه را با پارامتر اجرا کنید. در اولین اجرای برامه Help CLI نمایش داده میشود ``` dotnet FakeTrafficMaker.App.dll|exe --help ```

## Run Locally / توسعه
Install .Net SDK 8.0, VsCode or Visual Studio 2022

Clone the project
```bash
  git clone https://github.com/goombooli/DecoyTrafficMaker.git
```
Go to the project directory
```bash
  cd DecoyTrafficMaker
```
build program and restore packages

```bash
  dotnet build FakeTrafficMaker.sln
```
Run / Debug the program

```bash
  dotnet run FakeTrafficMaker.sln
# Or With Docker: docker compose up -d
```
Publish the program

```bash
  dotnet publish -c Release FakeTrafficMaker.sln
```
