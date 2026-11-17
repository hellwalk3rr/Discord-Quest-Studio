# Discord Quest Studio






**Discord Quest Studio** — нативное десктопное приложение для Windows (C# / WinForms + GDI+) для автоматического выполнения игровых, видео и activity-квестов Discord без скачивания игр.

---

## Возможности / Features

* **Поддержка всех типов квестов / All Quest Types:**
  * `PLAY_ON_DESKTOP` — эмуляция процесса игры через локальный host без скачивания самой игры + отправка heartbeat.
  * `WATCH_VIDEO` / `WATCH_VIDEO_ON_MOBILE` — автоматическое прохождение видео-квестов через `/video-progress`.
  * `PLAY_ACTIVITY` / `STREAM_ON_DESKTOP` — эмуляция активности и стримов через `/heartbeat`.
* **Автопринятие и автополучение наград / Auto-Enroll & Auto-Claim:**
  * Автоматическое принятие доступных заданий (`POST /quests/{id}/enroll`).
  * Автоматический сбор наград (`POST /quests/{id}/claim-reward`) после завершения квеста + кнопка **«Забрать награды»** (`Claim Rewards`).
* **Очередь заданий и живой прогресс / Quest Queue & Live API Sync:**
  * Режим **«Очередь всех»** (`Queue All`) поочерёдно выполняет все доступные квесты.
  * Синхронизация каждые 15 секунд, реальный баланс Orbs и живой прогресс из Discord API.
  * Кэширование базы `/applications/detectable` через `ETag` (`304 Not Modified`).
* **Мультиаккаунт и безопасность / Multi-Account & Security:**
  * Автоматический поиск токенов в локальных клиентах (`Discord`, `Discord Canary`, `Discord PTB`) с окном выбора аккаунта.
  * Шифрование токена и пароля прокси по стандарту **PBKDF2-HMAC-SHA256** (`vault.dat`).
* **Встроенный HTTP/HTTPS-прокси / Built-in HTTP/HTTPS Proxy:**
  * Поддержка `ip:port` и `ip:port:user:pass` с проверкой пинга в приложении.
* **Двуязычный интерфейс / Bilingual UI (`RU / EN`):**
  * Выбор языка в мастере установки и переключение на лету в шапке приложения и в трее.

---

## Установка / Installation

1. Скачайте **`DiscordStudioSetup.exe`** из раздела \[suspicious link removed\] (или запустите `DiscordStudioSetup.bat` для сборки установщика на месте через встроенный `csc.exe`).
2. Выберите язык (`Русский` / `English`) и следуйте шагам мастера установки.
3. Откройте **«Подключить Discord»** → нажмите **«Найти токен автоматически»** или вставьте токен вручную по встроенной инструкции во вкладке **«Гайд»**.

### Ручная компиляция из исходников / Build from Source

Требуется только Windows с предустановленным `.NET Framework 4.0+` (никаких внешних SDK не нужно):

```cmd
%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /out:DiscordQuestStudio.exe DiscordQuestStudio.cs
```

---

## Лицензия / License

© 2026 **hellwalk3rr** (Discord: `hellwalk3rr`).  
Программа распространяется **бесплатно** для личного некоммерческого использования. Продажа и коммерческое распространение запрещены.
