---
name: project-unity-platformer-shop
description: Unity 2D-платформер — архитектура UI и система магазина улучшений
metadata:
  type: project
---

Unity 2D-платформер (волны врагов, порталы, уровни, монеты с врагов).

**Ключевая архитектурная особенность:** ВЕСЬ UI строится из кода в рантайме (нет UI-префабов/настройки в редакторе). `fGameManager` (класс называется именно `fGameManager`, не `GameManager`), `CoinManager`, `AudioManager`, `MenuManager` создают свои Canvas/тексты/кнопки программно. Любой новый UI тоже надо строить кодом.

**Менеджеры-синглтоны:** `fGameManager.Instance` (пауза через `Time.timeScale`, `IsPaused`, `PauseForShop`/`ResumeFromShop`), `CoinManager.Instance` (`CoinCount`, `SpendCoins`, событие `OnCoinsChanged`), `AudioManager.Instance` (`PlaySfx`, `PlayCoin`). `fGameManager.EnsureRuntimeSystems()` авто-создаёт менеджеры (там же создаётся `ShopUI`).

**Магазин улучшений** (добавлен): `UpgradeManager` — статический класс, хранит уровни 3 улучшений (WeaponDamage/AttackSpeed/Invincibility) в статических полях = сессионное сохранение (переживает смену сцен, сбрасывается при выходе; по решению пользователя — БЕЗ записи на диск). Эффекты читаются игроком: `WeaponDamageMultiplier` (Bullet.damageMultiplier + melee), `AttackSpeedMultiplier` (делитель fireRate/meleeCooldown в PlayerAttack), `BonusInvincibilityTime` (PlayerHealth.InvincibilityCoroutine). `ShopUI` строит HUD-корзину + баланс и окно магазина.

**Спрайты магазина** в `Assets/Resources/Sprites/` импортированы как Sprite, но в режиме **Multiple** (под-ассеты) — грузить с фолбэком `Resources.LoadAll<Sprite>`. Цепочка урона внутри — `float` (`HealthSystem`/`Enemy.TakeDamage(float)`).

**Магазин — 5 улучшений:** базовые WeaponDamage/AttackSpeed/Invincibility + Freeze (❄️, каждая 2-я пуля, замедление + голубой тинт врага) и Poison (☠️, урон во времени + зелёный тинт). Эффекты пуль: `Bullet` несёт флаги applyFreeze/applyPoison; `Enemy.ApplyFreeze/ApplyPoison`, статус-цвет ведёт `Enemy.UpdateStatusEffects()` (приоритет: вспышка>заморозка>яд, иначе базовый tintColor). `PlayerAttack._shotCount % 2` помечает заморозку. Иконки freeze/poison — спрайты `Freezing bullets`/`Poisonous bullets`.

Окно магазина 760×940 (5 рядов) — может обрезаться на экранах ниже ~960px (CanvasScaler = ConstantPixelSize). Числовой баланс врагов/монет крутится в `EncounterManager` (baseEncounters/restAfterClear) и `CoinManager` (dropChance/min-maxCoinsPerEnemy).

Всё по ТЗ магазина реализовано (Фазы 0-4).

**Управление атакой:** выстрел — Space (без Shift); ближний бой — Shift+Space (а также ЛКМ/J/Enter). Ближний бой даёт процедурный эффект взмаха `SlashEffect.cs` (дуга-слэш, генерится кодом, без спрайтов). Базовая логика удара в `PlayerAttack.MeleeAttack` (урон по зоне + knockback + HitSpark + pogo).

**Ворота/портал** (`LevelManager.SpawnPortal`): теперь СТАТИЧНЫ — спавнятся активными сразу при загрузке уровня, стоят в конце (maxX-5), игрок вбегает и переходит (`Portal.OnTriggerEnter2D`→`GoToNextLevel`). НЕ зависят от зачистки врагов (раньше открывались из EncounterManager после всех энкаунтеров — это убрано). Энкаунтеры остались как бои по пути. Размеры уровней в СЦЕНЕ (MainScene.unity, компонент LevelManager): levelMaxX=[150,150,150], levelEnemyMaxX=[145,145,145]. location2 = индекс 1. Обычные уровни (0): фон растягивается под длину через `FitBackgroundWidth`.

**Level 3 (индекс 2) = «Катакомбы»:** склейка 5 спрайтов `Sprites/Катакомбы1..5` (в Resources/Sprites/, кириллица; 3974×2137, PPU 100) через `LevelManager.BuildCatacombsLevel()` (по образцу BuildLocation2Level). Двухъярусный: верхний коридор → ОБРЫВ (cliffX в 3-й картинке) → падение на нижний пол (подвал) → ворота = GAME WIN (последний уровень). Две группы картинок по вертикали: верхняя (индексы < catacombsLowerGroupStart=3 → Катакомбы1-3) ставится по catacombsUpperFloorY, нижняя (4-5) по catacombsLowerFloorY; каждая картинка позиционируется по СВОЕЙ доле пола из массива `catacombsFloorFractions` (попер-спрайт, [0.40,0.40,0.40,0.14,0.27]) — её нарисованный пол ложится на целевой мировой Y группы (как location2FloorFractions). Это чинит баг, где единая доля 0.22 ставила пол К4/К5 в пустоту и игрок «висел на чёрном». Подложка катакомб — тёмно-каменная (0.16), не почти-чёрная. Два BoxCollider: верхний пол [start..cliffX] на upperFloorY, пол подвала [cliffX..end] на lowerFloorY (игрок падает в обрыв и приземляется). Тюнинг-поля в инспекторе LevelManager: catacombsUpperFloorY/LowerFloorY/UpperFloorFrac/LowerFloorFrac/CliffSegFrac/LowerGroupStart/CameraSize/HeightScale. Камера: клампы _catacombsCamMinY/MaxY внутри арта. ЕЩЁ НЕ СДЕЛАНО: уступы-«бордюрчики» (промежуточные платформы для постепенного спуска) — добавить после подтверждения базовой геометрии. SafetyFloor и FitBackgroundWidth для индекса 2 отключены. Камера: catacombsCameraSize + клампы Y внутри склейки. Спавн игрока для катакомб переопределяется на upperFloorY. Страховка от падения: `Enemy.ForceDespawn()` (ниже Y=-40) + широкий `SafetyFloor` (BuildLevelBounds) на groundY. Есть диагностические Debug.Log — убрать позже.

**Важно про землю:** видимая земля сцены («Ground») КОРОЧЕ настроенной длины уровня (levelMaxX≈105, портал на maxX-5=100), из-за чего игрок проваливался у конца и не доходил до портала. Фикс: `LevelManager.BuildLevelBounds(levelIndex)` строит невидимый сплошной пол (BoxCollider2D) на всю длину minX..maxX + боковые стены, на слое земли сцены. Определение «на земле» (`PlayerMovement.IsGroundBelow`) принимает любой нетриггерный коллайдер. location2 пропускается (свой EdgeCollider-пол).
