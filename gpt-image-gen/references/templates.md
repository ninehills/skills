> [返回 README 首页](../README.md) | [完整画廊总览](./gallery.md) | [声明与公众号](./disclaimer.md)

<a name="section-templates"></a>

## 🧩 工业级提示词模板与防坑指南

> 大家好，我是你们的无情逆向机器。为了让大家能直接“开箱即用”，我把这 378 个案例扒了个底朝天，硬生生提炼出了 18 套**工业级提示词模板**。
> 说实话，整理这些规则差点给我干废了，但跑通之后真的很香！每一套模板都自带“防坑指南”，直接复制填空，再也不用玄学抽卡了。

<a name="tpl-ui"></a>

### UI与界面

**常规模板**

```text
为[产品类型]生成一张[平台，如 iOS/Android/Web]界面图。
核心功能：[功能点A]、[功能点B]、[功能点C]。
视觉风格：[极简/科技/拟物]，主色[颜色]，强调色[颜色]。
布局：[顶部导航/双栏/卡片流]，信息层级清晰，留白充足。
输出：高保真UI截图，文字清晰可读，比例[9:16/16:9]。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "UI Screenshot",
  "platform": "iOS",
  "product": "Fitness App",
  "layout": "Card-based feed with bottom tab bar",
  "style": {
    "theme": "Dark Mode",
    "primary_color": "Neon Green",
    "typography": "Clean sans-serif"
  },
  "content": {
    "header": "Today's Activity",
    "cards": [
      {"title": "Running", "data": "5.2 km", "button": "Start"},
      {"title": "Calories", "data": "340 kcal"}
    ]
  },
  "constraints": "High fidelity, readable text, 9:16 aspect ratio"
}
```

**避坑指南**

- **不要给模糊指令**：明确“平台 + 比例 + 布局”，否则模型会像个实习生一样乱排版。
- **强制文字锁定**：要求“文字绝对可读，必须显示指定的中文”，避免出现乱码按钮和毫无意义的火星文。

<a name="tpl-infographic"></a>

### 图表与信息可视化

**常规模板**

```text
生成[主题]信息图，目标读者为[人群]。
结构：标题区 + [3-5]个模块（每模块含图标、短标题、1-2句说明）。
图表类型：[流程图/对比图/关系图/时间线]。
风格：[专业报告/科普插画]，主色[颜色]，背景[浅色/深色]。
输出：信息层级清晰、可读性高的中文信息图。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Infographic",
  "topic": "Urban Metabolism",
  "audience": "General Public",
  "structure": {
    "title_area": "城市生命系统图谱",
    "layout": "Isometric cutaway, 12 numbered panels",
    "modules": [
      {"title": "能源", "icon": "lightning", "text": "Power flows"},
      {"title": "水循环", "icon": "water_drop", "text": "Water flows"}
    ]
  },
  "style": {
    "aesthetic": "Scientific atlas",
    "colors": "Low saturation, color-coded flows",
    "background": "Light paper texture"
  },
  "constraints": "No cyberpunk, no gibberish text, strict structural layout"
}
```

**避坑指南**

- **控制模块数量**：强制定制“模块数量”和“图表类型”，能极大降低画面混乱和信息溢出。
- **文案克制**：图表场景优先使用短句文案，千万不要把大段正文塞进画面里，模型不是排版工人。

<a name="tpl-poster"></a>

### 海报与排版

**常规模板**

```text
设计一张[活动/产品/电影]海报，主题为[主题词]。
主视觉：[主体元素]，标题文案：[标题]，副标题：[副标题]。
版式：[居中/左对齐/对角构图]，风格：[复古/未来/极简]。
色彩：[主色 + 辅色]，氛围：[情绪关键词]。
输出：可用于社媒传播的高分辨率海报。
```

**运动商业 Campaign 模板**

```text
设计一张[运动项目/健身品类]商业 Campaign 海报。
主体：[运动员/模特/产品道具]，姿态：[坐姿/冲刺/挥拍/力量动作]。
核心道具：[球拍/哑铃/球鞋/球衣]，以夸张比例或对角构图成为视觉锚点。
版式：[单张强主视觉/三联画/数据涂鸦海报]。
大字标题："[主标题]"，辅助文案："[短句/数据/精神口号]"。
视觉风格：高端运动品牌广告，强光影，反光地面，干净构图，品牌化配色[主色+辅助色]。
约束：主体清晰，文字可读，色调统一，不要杂乱拼贴，不要生成错误运动器材。
输出：1:1 或 4:5，适合社媒传播的运动商业视觉。
```

**概念字体海报模板**

```text
Create ONE finished premium conceptual typography poster for the exact title:

"[标题/词语/短句]"

Single poster only. No moodboard, grid, presentation board, mockup, captions, prompt text, process sheet, or sample labels.

The title must be the dominant visual structure of the poster: huge, readable, powerful, and spelled exactly. Do not translate, shorten, replace, or misspell it. Do not add other large readable text.

Silently interpret the title's meaning, mood, cultural aura, symbolic associations, psychological tension, and visual rhythm. Turn that interpretation into one strong visual metaphor.

Typography is the hero. Design custom-looking letterforms whose weight, width, contrast, spacing, rhythm, distortion, negative space, edge quality, and ink texture express the temperament of the title. The type should feel intentionally designed, not like a default font.

If the title refers to a widely known person, make a large editorial portrait or half-body figure a major visual presence, occupying roughly 40%-70% of the composition. The figure should interact with the typography: overlapping the letters, emerging from them, being framed by them, casting shadows on them, breaking through them, or being partially hidden behind them.

For abstract or non-person titles, use a human figure, landscape, object, or atmospheric setting only when it strengthens the meaning. It must interact with the typography and deepen the concept, not decorate it.

Use a restrained 4-6 color system matched to the theme: dominant background color, primary typography color, figure / landscape tone, emotional accent color, muted support color, and subtle paper / ink texture tone.

Composition style: high-end editorial poster, museum-quality graphic design, dramatic scale, strong hierarchy, few elements, intelligent whitespace, bold flat color areas, sharp cropping, silkscreen / lithograph / risograph grain, paper fibers, subtle ink imperfections, refined visual tension.

Avoid generic word art, glossy 3D lettering, random icons, stock-photo realism, cluttered collage, excessive grunge, tourist clichés, official logos, copied slogans, copied campaign aesthetics, unrelated text, and misspelled typography.
```

**水墨双重曝光人物海报模板**

```text
生成一张[人物/角色/品牌主理人/运动员]的水墨双重曝光人物海报。
画幅：9:16 竖版，高级电影海报构图。
主体结构：
- 上半区：放大的人物头部、面部轮廓或半身剪影，形成最强识别锚点。
- 中下区：同一人物的全身或半身主体，姿态为[站姿/动作姿态/凝视镜头]。
- 剪影内部：融合[关键场景]、[象征物]、[叙事片段]、[环境纹理]，形成双重曝光叙事。
视觉连接：用云雾、水墨扩散、飞白边缘、负空间和柔和明暗过渡，把上方剪影、内部拼贴和下方主体连成一条从上到下的视觉动线。
风格：东方水墨美学 + 写实电影感，克制、高级、留白充足，层次丰富但不杂乱。
文字：可加入[标题/姓名/短句]，必须少量、可读、像海报题签而不是信息图说明。
约束：不要硬拼贴，不要把背景塞满，不要廉价武侠特效，不要复制真实海报版式，不要让剪影和主体互相抢焦点。
输出：海报级完成图，主体清晰，水墨边缘自然，叙事元素与人物身份强相关。
```

**自然科普海报模板**

```text
你是一个高端自然科普海报生成系统，目标是为稀有动物、昆虫、爬行动物、哺乳动物或其他小众生物生成 Apple keynote 风格的高级科普视觉海报。

整体视觉方向：
生成一张 9:16 竖版高级科普海报，画面采用极简、纯白、干净、现代、Apple 式产品发布海报语言。背景应为纯白或极浅灰白渐变，保持大量留白。整体设计应具备高级感、克制感、视觉冲击力和科学展示感。

核心设计原则：
1. 主体动物必须被极度放大，成为画面最强视觉中心。
2. 主体应具有强烈立体感、真实质感、高清细节和柔和棚拍光影。
3. 海报信息要少而准，避免拥挤。
4. 不使用传统信息图的卡片、圆角框、复杂底纹、淡黄色纸张质感或装饰性边框。
5. 底部信息区只使用四列极简 icon + 标题 + 短说明，通过细竖线分隔。
6. 文字排版要像高端发布会视觉，标题巨大，副标题克制，正文小而清晰。
7. 风格关键词：Apple-inspired, premium editorial, pure white background, hero subject, clean typography, minimal infographic, high-end science poster.

画面结构：
顶部左侧为标题区：
中文大标题：{中文物种名}
中文副标题：{一句有吸引力的物种定位}
细短横线
英文名：{英文物种名}
分布信息：主要分布：{分布区域}

中部与下中部为主体视觉：
生成一个超高清、真实、具有强烈立体感的 {中文物种名}。
主体应占据画面 50% 到 70% 的视觉面积。
主体姿态应具有展示性、力量感或识别度。
保持白色背景，不添加复杂自然环境。
可以保留少量必要承托物，例如树枝、岩石、雪地、沙土或木皮，但必须简洁。
主体要有真实阴影，使其像高级产品摄影一样立在画面中。

底部信息区：
用四个极简信息栏目展示科普信息。
每个栏目包含：
一个细线 icon
一个彩色小标题
一段 1 到 3 行短文字
栏目之间用极细浅灰竖线分隔。
不使用卡片框，不使用圆角背景，不使用大面积色块。

四个信息栏目：
栏目 1：
标题：{重点特征1标题}
说明：{重点特征1短说明}

栏目 2：
标题：{重点特征2标题}
说明：{重点特征2短说明}

栏目 3：
标题：{重点特征3标题}
说明：{重点特征3短说明}

栏目 4：
标题：{重点特征4标题}
说明：{重点特征4短说明}

底部总结句：
在最底部居中放置一句灰色小字总结：
{一句高级、克制、有记忆点的科普总结}

字体与排版：
中文标题使用大号黑色、高级、稳重、有力量感的字体。
副标题使用灰色，中等字号，字距略宽。
英文名使用小号灰色，简洁现代。
正文使用清晰现代中文字体，保持可读。
所有文字必须留有足够呼吸感。

色彩规范：
背景：纯白、极浅灰、轻微柔光渐变。
主标题：黑色或深石墨色。
副标题与正文：中性灰。
底部四个信息标题可使用低饱和强调色：
暖棕、冷蓝、松石绿、紫色、橙色。
颜色只用于 icon 和小标题，不要大面积铺色。

图像质量：
2K 高清质感，细节清晰，主体锐利，光影真实。
主体纹理必须可信，例如毛发、鳞片、甲壳、皮肤褶皱、羽毛或斑纹。
避免变形、错误肢体、错误解剖结构、模糊主体、低质贴图、塑料感、卡通感。

禁止项：
不要使用淡黄色旧纸背景。
不要使用复杂信息图网格。
不要使用圆角卡片。
不要使用厚边框。
不要使用大面积装饰图形。
不要添加无关 logo。
不要添加多余小字。
不要让主体太小。
不要让文字压住主体。
不要让底部信息区过度拥挤。
不要出现儿童科普风、卡通风、低端展板风。

最终输出：
生成一张 9:16 竖版、高级、干净、强视觉冲击的 Apple 风自然科普海报。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Movie Poster",
  "theme": "Interstellar Journey",
  "typography": {
    "headline": "BEYOND STARS",
    "subheading": "A New Era Begins",
    "layout": "Centered, bold cinematic font, bottom heavy"
  },
  "visuals": {
    "subject": "Silhouette of an astronaut looking at a glowing nebula",
    "style": "Cinematic lighting, high contrast, dramatic shadows",
    "color_palette": "Deep space blue, glowing orange accents"
  },
  "vibe": "Epic, mysterious, vast"
}
```

**避坑指南**

- **不要偷懒**：写清“主视觉到底是什么玩意儿”，不要只丢一句“做一张海报”就指望出神图。
- **文案硬编码**：主标题与副标题都要写死，否则模型会给你疯狂加戏，自动瞎编不知所云的文字。
- **运动海报先定结构**：运动 Campaign 最容易变成杂乱拼贴，先锁定“单主视觉 / 三联画 / 数据涂鸦”再写主体和文案。
- **道具要当构图骨架**：球拍、哑铃、球鞋这类道具最好指定角度、比例和位置，否则模型容易把它们画成普通背景装饰。
- **字体海报先锁标题**：概念字体海报必须明确“标题必须拼写完全正确且成为主视觉”，否则很容易变成漂亮但不可读的字效图。
- **图像要和字互动**：人物、物体或场景必须嵌入、遮挡、穿过或托起字形，只摆在旁边会像装饰素材。
- **禁止 moodboard 化**：明确要求 single poster only，避免模型生成多方案展示板、过程稿或样张拼贴。
- **主体放大**：自然科普海报中，主体动物必须被极度放大，占据画面 50%-70% 的视觉面积，确保成为最强视觉中心。
- **信息克制**：遵循“少而准”原则，底部信息区只使用四列极简布局，避免信息拥挤和视觉混乱。
- **风格统一**：严格遵循 Apple 式极简风格，使用纯白背景、干净排版和柔和棚拍光影，避免传统信息图的卡片、圆角框等元素。

### 商品与电商

**常规模板**

```text
生成[商品名]电商主图，卖点为[卖点1]、[卖点2]。
场景：[纯色棚拍/生活方式场景]，镜头：[特写/半身/全景]。
材质细节：[材质关键词]，灯光：[柔光/侧光/轮廓光]。
附加元素：[价格角标/卖点icon/促销文案]。
输出：电商平台可直接使用的商品展示图。
```

**个人化美妆推荐报告模板**

```text
你是一个专业美妆顾问 + 人脸分析系统 + 品牌视觉设计系统。
目标：基于[用户自拍]与[口红品牌]，生成一张具有“分析 + 推荐 + 试色 + 场景建议”的竖版口红推荐报告信息图。

输入参数：
用户图像：[用户自拍]
品牌：[Dior / YSL / Armani / Chanel / TF / 其他品牌]
风格偏好（可选）：[通勤 / 温柔 / 气场 / 氛围感 / 显白优先]
推荐数量：[3-5]

分析层：
- 判断肤色：冷 / 暖 / 中性（含明度）
- 判断气质：清冷 / 温柔 / 明艳 / 干净 / 成熟
- 判断唇部基础：唇色深浅、唇形、适合浓淡
- 输出一句总结：「更适合 [色系] + [饱和度] + [质地] 的口红方向」

推荐层：
从[品牌]中筛选[3-5]个差异化色号，每个色号包含：
- 色号名称
- 色系标签
- 上脸效果
- 推荐场景

品牌视觉层：
根据[品牌]自动生成视觉调性，只用少量品牌强调色做标题、细线、小 icon 和局部点缀。
示例：YSL 黑金强对比，Dior 法式柔光灰白，Armani 低饱和雾面，Chanel 极简黑白，TF 深色电影感。

版式结构：
左上：用户输入图 + 肤色分析
右上：一句分析结论
中部：3-5 个同一张脸的唇色试色矩阵，每列一个色号
底部：有判断力的个人建议

视觉要求：
高端美妆编辑视觉，结构化信息可视化排版，真实皮肤质感，唇色精准，统一光影，9:16 竖版，8K。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "E-commerce Hero Image",
  "product": {
    "name": "Noise Cancelling Headphones",
    "material": "Matte black finish with metallic accents",
    "angle": "3/4 profile, floating slightly"
  },
  "setting": {
    "background": "Minimalist studio setup, soft gray gradient",
    "lighting": "Softbox overhead, sharp rim light on edges"
  },
  "copywriting": {
    "badges": ["NEW", "$299"],
    "slogan": "Silence the World"
  },
  "constraints": "Commercial photography quality, hyper-realistic textures"
}
```

**避坑指南**

- **材质和光影是灵魂**：一定要堆叠材质（如“磨砂质感”）和灯光（如“轮廓光”）的关键词，商品图一旦没有光影，立刻变成地摊货。
- **别把促销贴满全屏**：文案只给核心的 1-2 句（如“新品上市”），字多了画面就毁了。
- **先分析再出图**：美妆推荐类不要直接让模型摆色号，先要求它分析肤色、气质、唇部基础，再把结论映射到色号推荐。
- **品牌只做点缀**：品牌调性应该体现在细线、强调色、字体气质和光影里，不要把 logo 或大色块铺满画面。
- **试色矩阵要锁定同一张脸**：明确“同一张脸，仅唇色变化”，否则模型容易把每个色号都画成不同的人。

### 品牌与标志

**常规模板**

```text
为[品牌名]设计品牌视觉方案。
品牌关键词：[关键词1]、[关键词2]、[关键词3]。
包含：Logo方向[几何/字标/图形]、辅助图形、主辅色、应用示意。
风格：[现代/高级/亲和]，行业：[行业]，受众：[受众]。
输出：统一风格的品牌识别视觉图。
```

**完整品牌身份包模板**

```text
你是顶级品牌代理创意总监，目标是为[业务/产品]交付一套覆盖 Logo、配色、字体、语调和应用触点的完整品牌身份系统。

输入信息：
业务名称：[业务名]
业务描述：[一句话说明]
行业：[行业]
目标受众：[详细描述]
竞争对手：[3-5个]
品牌个性：[5个关键词]
希望触发的感受：[信任 / 兴奋 / 奢华 / 亲近 / 力量 / 其他]
喜欢的视觉身份：[3个参考]
讨厌的视觉身份：[3个反例]
设计预算：[免费 / 付费]

请输出：
1. 品牌战略基础：品牌原型、核心承诺、定位、差异化和唯一关键词。
2. Logo 概念：生成 3-5 个完全不同的 Logo 方向，每个方向说明核心视觉理念、形状语言、象征意义、字体方向、第一眼情绪和适用触点。
3. 配色系统：主色、辅助色、强调色、中性色、HEX 代码、心理学解释、使用规则和禁用搭配。
4. 字体系统：标题字体、正文字体、强调字体、字号层级、字距、行高和免费替代方案。
5. 应用触点：名片、App 图标、网站首页、社媒模板、广告牌或包装上的应用效果。
6. 品牌规则：3 条永远不要打破的核心品牌规则。

输出形式：
结构化品牌手册，任何设计师、开发者或 AI 工具都能在 10 分钟内理解并复用。
```

**品牌触点系统视觉板模板**

```text
为[品牌名]生成一张高端品牌触点系统视觉板，不是单张海报，而是一套完整品牌应用展示。

品牌定位：[行业/生活方式/产品品类]
核心气质：[关键词1]、[关键词2]、[关键词3]
主视觉场景：[核心产品/服务/体验]，放在[材质表面/空间场景]中，使用[光线]和[镜头]呈现。

触点系统必须包含：
- 主产品 hero shot
- 包装盒 / 手提袋 / 杯子 / 标签 / 贴纸 / 封签等品牌物料
- 菜单卡 / 价目表 / 小型排版样张
- 生活方式场景或用户使用片段
- 配色、字体、图形语言在不同触点上的统一应用

设计语言：
[现代极简/日式留白/奢华编辑/科技品牌]，主色[颜色]，辅助色[颜色]，大量留白，细腻材质，真实阴影，微小文字清晰可读。

构图要求：
像顶级设计机构提案页，所有触点整齐但不死板，主视觉最突出，辅助物料层级清楚，整体有品牌系统感和可落地感。

约束：
不要只生成一个 logo；不要把所有物料挤成杂乱拼贴；不要使用随机乱码文字；不要让包装、菜单、贴纸彼此风格割裂。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Brand Identity Design",
  "brand": {
    "name": "Nova Dynamics",
    "industry": "AI Technology",
    "keywords": ["Innovative", "Minimalist", "Trustworthy"]
  },
  "deliverables": [
    "Logo mark (geometric fusion of a neural network node and a star)",
    "Color palette (Electric Blue and Pure White)",
    "Business card mockup"
  ],
  "style": "Modern corporate, flat vector, high contrast",
  "constraints": "No gradients, scalable vector style, clean white background for logo"
}
```

**避坑指南**

- **做减法**：先定义品牌关键词，再要求视觉输出，结果更统一。别让它画“一条喷火的龙缠绕在长城的柱子上还带着闪电”，那不叫 Logo，那叫插画。
- **强制背景**：必须强调“纯白背景（Pure White Background）”，方便后期抠图。
- **先做品牌战略再画 Logo**：如果缺少目标受众、竞争对手和情绪目标，Logo 很容易只是漂亮图形，无法解释为什么适合这个品牌。
- **Logo 必须看应用场景**：要求同时展示名片、App 图标、网站、广告牌等触点，能快速发现缩小后不可读、横竖比例不适配等问题。
- **品牌手册要写禁用规则**：除了给颜色和字体，也要写“不要怎么用”，否则后续延展很容易把统一性弄丢。

### 建筑与空间

**常规模板**

```text
生成[空间类型]设计效果图，功能定位为[用途]。
风格：[现代简约/工业/新中式]，材质：[木/石/金属/玻璃]。
空间结构：[开敞/分区]，动线：[主通道说明]。
光线：[自然采光/人工照明方案]，时间：[白天/夜景]。
输出：写实建筑空间渲染图。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Architectural Visualization",
  "space": {
    "type": "Modern Cabin Interior",
    "function": "Living room",
    "materials": "Exposed concrete, large floor-to-ceiling glass, warm timber accents"
  },
  "environment": "Nestled in a dense, snowy pine forest visible through the glass",
  "camera": {
    "angle": "Eye-level perspective, wide-angle lens",
    "lighting": "Golden hour, warm interior lights glowing, cool blue ambient light outside"
  },
  "render_quality": "Unreal Engine 5 style, hyper-realistic, 8k resolution, ray tracing"
}
```

**避坑指南**

- **控制视角**：建筑图最容易翻车的就是透视变形。用“Eye-level perspective（人眼视角）”能压住它。
- **冷暖对比**：室外的冷光（蓝/灰）和室内的暖光（黄/橙）搭配，是提升空间高级感的作弊码。

<a name="tpl-photo"></a>

### 摄影与写实

**常规模板**

```text
拍摄主题：[人物/物品/街景]，场景为[地点]。
摄影参数风格：[35mm/85mm]，[浅景深/深景深]，[纪实/电影感]。
光线：[自然光/夜景霓虹/逆光]，情绪：[情绪词]。
细节要求：[肤质/材质/颗粒感]。
输出：高写实摄影风格图像。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Hyper-realistic Photography",
  "subject": {
    "description": "A weary 30-year-old barista wiping a coffee cup",
    "details": "Subtle sweat on forehead, detailed skin pores, wearing a denim apron"
  },
  "setting": "Dimly lit vintage cafe, rain visible through the window behind",
  "camera_specs": {
    "gear": "Shot on Sony A7R IV, 50mm lens",
    "aperture": "f/1.4 (shallow depth of field, background completely blurred)",
    "lighting": "Cinematic lighting, neon sign reflecting on wet window, soft rim light on subject's hair"
  },
  "film_aesthetic": "Kodak Portra 400 emulation, subtle film grain"
}
```

**街头意外瞬间写实摄影模板**

```text
生成一张竖版手机纪实照片，主题是[意外事件/日常瞬间]发生在[街头/室外地点]。
主体：[物品/人物动作/现场痕迹]，必须呈现真实的材质状态，例如[液体扩散/冰块散落/纸张褶皱/灰尘颗粒]。
环境：[地面材质/墙面/街景元素]，保留自然杂乱和生活痕迹。
光线：[正午强光/阴天散射光/夜间路灯]，阴影要符合真实方向，可加入[人物影子/路牌影子/树影]。
镜头：手持手机视角，略微俯拍或低角度，构图自然，像随手拍到的现场。
画面质感：raw unedited photo look，自然色彩，真实纹理，高细节。
负面约束：不要插画、动漫、CGI、棚拍光、过度干净、过度构图、假液体、漂浮物、品牌文字、水印、海报设计感。
输出：一张可信的日常纪实摄影图。
```

**避坑指南**

- **加点瑕疵**：AI 画的人太完美了，反而像假人。加入“皮肤纹理（skin pores）”、“雀斑”、“轻微胶片颗粒（film grain）”，真实感瞬间拉满。
- **用参数说话**：用 `f/1.4` 代替“浅景深”，用 `50mm` 代替“半身照”，大模型吃这套。
- **把“不完美”写具体**：写“粗糙石砖、散落冰块、自然阴影、轻微手持感”，比只写“真实”更稳定。

### 插画与艺术

**常规模板**

```text
创作[题材]插画，主角为[角色/主体]。
画风：[日漫/水彩/扁平/厚涂]，线条：[细腻/粗犷]。
配色：[配色方案]，背景：[简洁/复杂场景]。
构图：[近景/中景/远景]，重点表现[细节]。
输出：可用于封面或社媒发布的高质量插画。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Artistic Illustration",
  "art_style": "Studio Ghibli inspired anime style",
  "scene": {
    "description": "A giant flying whale carrying a small cozy village on its back",
    "details": "Windmills turning, tiny people looking over the edge, fluffy white clouds"
  },
  "palette": "Vibrant sky blue, lush greens, soft pastel accents",
  "technique": "Cel shading, detailed background art, soft glowing magical aura",
  "mood": "Whimsical, adventurous, nostalgic"
}
```

**避坑指南**

- **锁定笔触**：插画如果不限制笔触（如“厚涂”、“水彩晕染”），它通常会给你一种毫无灵魂的 AI 默认塑料风。
- **慎用大师名**：提大师名字很爽，但容易被模型原样照搬其代表作的构图。建议提取大师的特征（如“梵高的旋转星空笔触”），而不是直接写大师名。

### 人物与角色

**常规模板**

```text
设计[角色身份]角色设定图。
外观：[年龄/发型/服饰/配件]，性格：[关键词]。
姿态：[站姿/动态动作]，表情：[情绪]。
世界观：[时代/阵营/职业]，标志性元素：[元素]。
输出：角色主视图 + 风格统一的人设图。
```

**动作分解参考表模板**

```text
生成一张[角色/人物]动作分解参考表。
风格：[黑白线稿/3D 灰阶/漫画分镜/教学图]，背景纯净，技术参考图气质。
版式：4×4 网格，共 16 个等尺寸面板，细线分隔，每格左上角编号 1-16。
角色一致性：所有面板使用同一角色，保持脸型、服装、比例和发型一致。
每格结构：
- 顶部：动作标题
- 中央：完整身体动作姿态
- 底部：3-4 行动作说明
- 叠加：方向箭头、旋转箭头或运动轨迹线
动作序列：[从基础站姿到结束动作的完整步骤]
约束：不要复杂背景，不要新增角色，不要彩色干扰，不要改变角色身份。
输出：清晰可读、可用于动画/舞蹈/游戏动作参考的角色动作表。
```

**参考图转 3D 收藏玩具模板**

```text
将输入照片转换为高端 3D 收藏玩具形象。
身份保持：保留原始人物/角色的脸部身份、主要发型、表情气质和服装识别点。
造型比例：大头设计，五官轻微夸张，身体比例玩具化，但整体仍保持高级设计感。
材质：哑光 vinyl / resin / collectible figure finish，皮肤和服饰材质要有细节。
灯光与背景：柔和棚拍光，干净背景，[黑色/白色/品牌色]，主体居中，轮廓清晰。
质感：超清锐度，真实材质反射，8K render，premium designer toy aesthetic。
约束：不要改变身份，不要廉价塑料感，不要多角色，不要复杂背景，不要文字水印。
输出：一张完整的高端收藏玩具渲染图。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Character Concept Art",
  "character": {
    "identity": "Cybernetic Bounty Hunter",
    "appearance": "Short silver hair, glowing red synthetic left eye, athletic build",
    "attire": "Tactical trench coat with neon piping, holding a plasma rifle"
  },
  "pose": "Dynamic action stance, looking over shoulder with a smirk",
  "environment": "Rainy neon-lit alleyway background (blurred)",
  "style": "Concept art, sharp linework, vibrant cyberpunk palette"
}
```

**避坑指南**

- **拆解五官**：不要只写“很美的女孩”，大模型不知道你的审美标准。拆解成“桃花眼、高鼻梁、野生眉”。
- **服装材质**：写清衣服的材质（如“丝绸”、“机能防风面料”），能让角色立刻变得立体。
- **动作表要锁网格**：动作分解图必须明确面板数量、编号、每格结构，否则模型会把步骤挤成一张杂乱说明图。
- **玩具化要保留身份锚点**：先锁脸型、发型、服装识别点，再写大头比例和材质，能减少“变成另一个人”的概率。
- **角色一致性前置**：动作序列越长越容易换脸换衣服，要把“同一角色、同一服装、同比例”写在动作列表之前。

### 场景与叙事

**常规模板**

```text
生成[故事主题]场景图，发生在[时间+地点]。
主事件：[事件描述]，主角：[角色]，冲突点：[冲突]。
镜头语言：[广角建立镜头/中景叙事/特写]。
氛围：[紧张/温暖/悬疑]，色调：[冷/暖/高反差]。
输出：具备叙事张力的场景概念图。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Narrative Scene",
  "story_context": "The exact moment an ancient seal breaks",
  "environment": "Crumbling stone temple overgrown with glowing blue vines",
  "action": "A young explorer dropping their torch as a massive beam of light shoots into the sky",
  "atmosphere": {
    "mood": "Awe-inspiring, terrifying",
    "lighting": "Blinding central light casting long dramatic shadows"
  },
  "camera": "Low angle shot, emphasizing the scale of the light beam"
}
```

**避坑指南**

- **要有“动词”**：叙事图最怕画成风景明信片。一定要写“事件”（如“正在崩塌”、“刚点燃火把”），让画面动起来。
- **镜头语言**：使用“Low angle shot（低角度仰拍）”或“Dutch angle（倾斜镜头）”来增加戏剧冲突。

### 历史与古风题材

**常规模板**

```text
生成[朝代/古风设定]题材画面，主题为[主题]。
人物：[身份/服饰/器物]，场景：[宫廷/市井/山水]。
美术风格：[工笔/写意/影视写实]，色调：[色调]。
文化细节：[纹样/礼制/建筑要素]。
输出：历史氛围准确的古风题材图。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Historical/Oriental Scene",
  "setting": "Tang Dynasty Capital City at Night",
  "subject": {
    "identity": "Noblewoman",
    "clothing": "Traditional Ruqun (襦裙) with elaborate floral embroidery",
    "action": "Holding a glowing silk lantern, looking at fireworks"
  },
  "style": "Cinematic realism combined with subtle traditional ink wash (水墨) textures",
  "details": "Accurate Tang architecture, bustling crowd in background",
  "constraints": "No modern elements, historically accurate clothing structure"
}
```

**避坑指南**

- **拒绝大杂烩**：明确朝代（唐/宋/明），否则大模型会给你画出一个穿着和服、拿着清朝折扇在唐朝宫殿里的人。
- **强制排雷**：一定要加上“禁用现代元素（No modern elements）”，防止古风美女手里突然多出一杯星巴克。

### 文档与出版物

**常规模板**

```text
制作[文档类型，如菜单/杂志内页/报纸版式]。
版面结构：[栏数/页边距/标题层级]。
内容模块：[封面区/正文区/图表区/脚注]。
字体风格：[衬线/无衬线]，配色：[配色方案]。
输出：可读性强、版式规范的出版物视觉稿。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Editorial Layout",
  "document": "Fashion Magazine Double-page Spread",
  "grid": "3-column grid, wide margins",
  "content": {
    "left_page": "Full-bleed high-fashion photograph of a model in a red dress",
    "right_page": {
      "headline": "THE RED RENAISSANCE",
      "body_text": "(Simulated text blocks)",
      "pull_quote": "\"Color is power.\""
    }
  },
  "typography": "Elegant serif for headlines, clean sans-serif for body",
  "palette": "Monochrome with stark red accents"
}
```

**避坑指南**

- **结构优先**：明确“栏数（columns）”和“留白（margins）”比堆砌风格词更重要。
- **放弃全文**：不要指望大模型能排出一整页毫无错字的正文，让它用“模拟文本（Simulated text blocks）”填充正文，只写死大标题。

### 其他应用场景

**常规模板**

```text
任务目标：[你要生成的内容类型]。
输入约束：主体[主体]，场景[场景]，风格[风格]，色彩[配色]。
质量约束：清晰度[高清/4K]，比例[比例]，构图[构图方式]。
输出约束：用于[用途]，需突出[核心信息]。
请输出一版主方案 + 一版备选方案。
```

**概念产品研发拆解板模板**

```text
为[产品/家具/装置]生成一张完整的概念产品研发拆解板，而不是单张成品渲染图。

核心概念：
把[灵感来源，如揉皱纸团/贝壳/折纸/机械结构]转译成[产品类型]。
设计哲学：[一句话说明功能与情绪，例如“把受控混乱转化为高舒适度座椅”]。

画面结构：
中心：高质量 hero render，展示最终产品的主要形态、材质和比例。
左侧：观察与形态分析，包含灵感图、轮廓提取、结构线、折痕/纹理/受力方向标注。
中部：形态迭代过程，展示从原始形态到产品外壳的 3-5 个演化步骤。
下方：人体工学或使用场景验证，包含尺寸、角度、使用姿态和关键功能说明。
右侧：结构集成与材料方案，展示内部骨架、外壳、软垫/面料/连接件等分层拆解。
底部：最终材质、表面纹理、颜色方案和关键规格表。

视觉风格：
工业设计提案板，干净白底或浅灰背景，技术图纸 + 产品摄影混合风格，细线标注，清晰标题，真实阴影，材质细节可见。

约束：
不要只画一个漂亮产品；必须展示分析、迭代、人体工学、结构、材料和规格。
不要让文字挤满画面；每个阶段只保留短标题和关键标签。
产品外形应保留[灵感来源]的识别特征，但必须看起来可制造、可使用。
```

**JSON 进阶模板（推荐给 Agent 调用）**

```json
{
  "type": "Custom Generation",
  "objective": "Generate [Specific content]",
  "inputs": {
    "subject": "[Main subject details]",
    "scene": "[Background and context]",
    "style": "[Artistic/Visual style]",
    "palette": "[Color scheme]"
  },
  "quality_constraints": {
    "resolution": "8k, hyper-detailed",
    "aspect_ratio": "[e.g., 16:9]",
    "composition": "[e.g., Rule of thirds]"
  },
  "output_requirements": {
    "usage": "[Intended use case]",
    "focus": "[Key element to highlight]"
  }
}
```

**避坑指南**

- **先说干嘛的**：一上来先写“任务目标和用途”，让模型建立全局上下文，再写视觉细节。
- **A/B 测试**：通用场景建议在 prompt 里要求“一次生成主方案 + 备选方案”，方便你直接挑好的。

***

