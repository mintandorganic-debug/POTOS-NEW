Кандидаты на удаление
=====================

Папка с "~" в конце имени не импортируется Unity, поэтому файлы отсюда не дают ошибок в консоли.
Если файл понадобится, верните его на прежнее место. Если не понадобится, удалите всю папку.

1. Clock Dissolve Appear.prefab
   Было: Assets/Shaders/Assembly VFX Dissolve/Prefab/
   Причина: внутри вложен префаб "Hands_teethGold and White" (GUID 6c8611523e4e1dd49aea117aae14ab8c),
   которого нет в проекте. Ошибка: "Nested Prefab problem. Missing Nested Prefab Asset".
   Нигде в сценах, префабах и скриптах не используется.

2. FlameThrower.vfx
   Было: Assets/Shaders/Flame/
   Причина: ссылается на шейдер-граф с GUID dcff2a11524a7184c8850ca793dfce3b, которого нет в проекте.
   Рядом лежит Flame.shadergraph с другим GUID (97eb47d0c82bba444aede656d7d7ce14), ссылка сломана.
   Ошибка: "The VFX Graph 'FlameThrower' cannot be compiled because a Shader Graph ... is missing".
   Нигде в сценах, префабах и скриптах не используется.
