# Run SentinelPay in VS Code: a step-by-step guide for beginners

**Be warned:** this code has never been compiled. Your first build will probably show errors. That is normal and fixable: see Step 5 and `docs/CI_FIRST_BUILD.md`. Do the steps in order and stop at the first one that fails.

## What you are starting (three separate things)
1. **Helper services** in Docker: a database (PostgreSQL), a cache (Redis), a message broker (RabbitMQ).
2. **The backend** ("API"), written in C# on .NET 9. It talks to the helper services.
3. **The mobile app**, written in Flutter. It talks to the backend.

Start them in that order. Commands below are for **Windows PowerShell**; Mac/Linux differences are noted.

---

## Part A: Install the tools (about an hour, once)
1. **VS Code**: https://code.visualstudio.com
2. **.NET 9 SDK**: https://dotnet.microsoft.com/download. Open a new terminal and run `dotnet --version`. You should see `9.x.x`.
3. **Docker Desktop**: https://www.docker.com/products/docker-desktop (on Windows it needs WSL 2; the installer guides you). Start it and wait for "Engine running". Check: `docker --version`.
4. **Git**: https://git-scm.com (Windows users: this also gives you "Git Bash").
5. **Flutter SDK**: follow https://docs.flutter.dev/get-started/install for your system (extract to a folder without spaces, such as `C:\src\flutter`, and add `flutter\bin` to PATH). Check: `flutter --version`.
6. **Android Studio** (for the Android emulator): https://developer.android.com/studio. On first start, let it install the Android SDK. Then in a terminal run `flutter doctor --android-licenses` and accept (type `y`).
7. Run `flutter doctor`. Aim for ticks next to Flutter, Android toolchain and VS Code. (A missing Chrome, Visual Studio or Xcode item does not matter for this guide.)
8. **VS Code extensions** (Extensions icon on the left, search by name): *C# Dev Kit*, *C#*, *Dart*, *Flutter*, *Docker*, *REST Client*. When you open the project, VS Code may offer to install these for you; click **Install**.

## Part B: Open the project
1. Unzip `sentinelpay-scaffold.zip`.
2. VS Code: **File > Open Folder**, choose the `sentinelpay` folder, click **Yes, I trust the authors**.
3. Open a terminal inside VS Code: **Terminal > New Terminal**. All commands below run there.

## Part C: Start the helper services
1. Create your settings file and choose passwords:
   ```powershell
   Copy-Item .env.example .env          # Mac/Linux: cp .env.example .env
   ```
   Open `.env` and replace the three `change-me` passwords and the JWT key with your own text. **Write the passwords down; you need them again in step 3.**
2. Start Docker services:
   ```powershell
   docker compose up -d
   docker compose ps
   ```
   After about 30 seconds all three should say `healthy`. (Docker Desktop must be running.)
3. Create the API settings file and put the **same** passwords in it:
   ```powershell
   Copy-Item .env.api.example .env.api   # Mac/Linux: cp .env.api.example .env.api
   ```
   In `.env.api` replace `SAME_AS_PG_PASSWORD`, `SAME_AS_REDIS_PASSWORD`, `SAME_AS_RABBIT_PASSWORD` with the passwords from `.env`, and set `Jwt__SigningKey` to any text of 32 or more characters.

Check: open http://localhost:15672 in a browser (RabbitMQ screen). User `sentinel`, password = your RabbitMQ password.

## Part D: Build and run the backend
Step 5 is where errors most likely appear.

1. Go to the backend folder and restore packages (downloads libraries):
   ```powershell
   cd backend
   dotnet restore src/SentinelPay.Api
   ```
2. **Step 5: build it.**
   ```powershell
   dotnet build src/SentinelPay.Api
   ```
   - `Build succeeded` with 0 errors: great, go on.
   - Errors: copy the lines containing `error` and send them to me. Common causes are listed in `docs/CI_FIRST_BUILD.md` (wrong package version names are the usual first one). Fix the first error, rebuild, repeat; later errors often disappear when earlier ones are fixed.
3. Go back to the project root and run the API with the debugger: `cd ..`, then press **F5** (Run > Start Debugging) and choose **API (Development)**. Or without the debugger:
   ```powershell
   cd backend/src/SentinelPay.Api
   Get-Content ../../../.env.api | ForEach-Object { if ($_ -match '^\s*([^#=]+)=(.*)$') { Set-Item -Path "Env:$($Matches[1])" -Value $Matches[2] } }
   dotnet run
   ```
   (Mac/Linux: `set -a; source ../../../.env.api; set +a; dotnet run`.)
4. Check: open http://localhost:8080/health. You should see `Healthy`. In Development the database tables are created automatically on first start.

## Part E: Try the API with no app (recommended before Flutter)
1. Open `requests.http` in VS Code (the REST Client extension adds a **Send Request** link above each `###` section).
2. Click **Send Request** on sections **1, 2, 3, 4** in order.
   - Section 2 creates a test account and a login token (this only exists in Development).
   - Section 3 makes a payment. In the default **Hybrid** mode the answer is `202 Accepted`.
   - Section 4: click it a few times. The status can change from `PendingRisk` to `Settled` as the background steps finish. Settling needs RabbitMQ running.
3. Try section 5 twice: the same payment comes back, never two charges.
4. Sections 6-8 show the agent decisions and the run logs, the same data the app's **Run logs** screen will show.
5. Look in `backend/src/SentinelPay.Api/logs/`: the rotating log files are there.

To try another risk mode, change `Risk__Mode` in `.env.api` (Off, Sync, Async, Hybrid), stop and restart the API, repeat section 3.

## Part F: Run the automated tests
```powershell
cd backend
dotnet test tests/SentinelPay.Domain.Tests
dotnet test tests/SentinelPay.Application.Tests
dotnet test tests/SentinelPay.Agents.Tests
dotnet test tests/SentinelPay.Integration.Tests    # needs Docker running; starts its own temporary database
```
Or use VS Code's **Testing** icon (the flask) to run them with buttons. Test failures are expected on the first run; each one tells you what it expected.

## Part G: Run the Flutter app on an Android emulator
1. **Create an emulator**: in Android Studio, *More Actions > Virtual Device Manager > Create Device* (pick a Pixel phone and a recent Android image). Start it.
2. **Create the Android project files.** This repo contains only the Dart code. In the VS Code terminal:
   ```powershell
   cd client
   Copy-Item -Recurse lib ../lib_backup
   flutter create . --platforms=android --project-name sentinelpay
   ```
   Check that `client/lib/main.dart` still contains the lines `_apiBase` and `_skipBiometric`. If not, copy it back from `lib_backup`.
3. **Allow plain http for local testing** (this is for the emulator only). Open `client/android/app/src/main/AndroidManifest.xml` and:
   - inside `<manifest ...>` add `<uses-permission android:name="android.permission.USE_BIOMETRIC"/>` and `<uses-permission android:name="android.permission.INTERNET"/>`;
   - on the `<application` tag add `android:usesCleartextTraffic="true"`.
   Then open `client/android/app/build.gradle.kts` (or `build.gradle`) and set `minSdk = 23`.
   (If you later turn biometrics on, `MainActivity` must extend `FlutterFragmentActivity`; the development switch below skips biometrics, so you do not need this yet.)
4. **Get your test token.** In `requests.http` send section 2 again and copy two values from the response: `opsToken` and `accountId`.
5. Open `.vscode/launch.json`, find **Flutter app (emulator, dev settings)** and replace `PASTE_opsToken_HERE` and `PASTE_accountId_HERE` with those values. (`10.0.2.2` is how the emulator reaches your computer; do not change it. `SKIP_BIOMETRIC=true` skips the fingerprint prompt in development only.)
6. Install packages and run:
   ```powershell
   flutter pub get
   ```
   Pick your emulator in the bottom-right device selector of VS Code, open the **Run and Debug** panel, choose **Flutter app (emulator, dev settings)** and press the green button (F5). The first build takes several minutes.
7. In the app: **Make a payment**, enter an amount and `shop-1`, tap **Pay**. Then open **Run logs (operations)**: you should see the same request, and tapping a line traces it.

Run the client tests: `cd client` then `flutter test`.

## Troubleshooting
| Problem | Fix |
|---|---|
| `docker compose` says it cannot connect | start Docker Desktop and wait until it says running |
| `.env` error "set PG_PASSWORD" | you did not copy `.env.example` to `.env` or left a value empty |
| API starts then crashes about Jwt:SigningKey | `Jwt__SigningKey` in `.env.api` must be at least 32 characters |
| API crashes connecting to the database or Redis | passwords in `.env.api` do not match `.env`; run `docker compose ps` |
| Payment stays `PendingRisk` | RabbitMQ not running or wrong password; look at the console and `logs/` |
| `/health` works but port in use error | something else uses 8080; change `ASPNETCORE_URLS` in `.env.api` and `API_BASE` in launch.json |
| App cannot reach the API | use `10.0.2.2`, not `localhost`; API must be running; cleartext setting added |
| App shows "session expired" | the token in launch.json is old (valid 8 hours) or pasted wrongly; send section 2 again |
| Run logs says you need the operations role | you pasted `token`, not `opsToken` |
| `flutter doctor` complains about licenses | `flutter doctor --android-licenses` |
| Build errors in .NET | send the `error` lines to me; see `docs/CI_FIRST_BUILD.md` |

## Safety reminders
- `/dev/seed` makes tokens without a login. It exists only when `ASPNETCORE_ENVIRONMENT=Development`. **Never deploy with that setting.**
- Never commit `.env` or `.env.api`.
- This is a learning and development setup. It is not secure enough for real money: the checklist is in `docs/` (security documentation, known gaps).
