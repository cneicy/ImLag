using System;
using System.Collections.Generic;
using CommonSDK.Event;

namespace ImLag.GUI.Scripts.Core;

public class LanguageChangedEvent : EventBase
{
    public string Language { get; init; } = LocalizationManager.DefaultLanguage;
}

public static class LocalizationManager
{
    public const string DefaultLanguage = "zh-CN";
    public const string TraditionalChinese = "zh-TW";

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["zh-CN"] = new()
        {
            ["language.label"] = "界面语言",
            ["language.zh-CN"] = "简体中文",
            ["language.zh-TW"] = "繁体中文",
            ["mode.cfg"] = "CFG模式",
            ["mode.chat"] = "聊天模式",
            ["mode.cfg_description"] = "生成游戏配置文件，检测到死亡后自动按下绑定的快捷键发送消息",
            ["mode.chat_description"] = "检测到死亡后自动模拟按键发送消息",
            ["section.general"] = "通用设置",
            ["section.cfg"] = "CFG 模式设置",
            ["section.chat"] = "聊天模式设置",
            ["section.corpus"] = "语料管理",
            ["general.title"] = "通用设置",
            ["general.hint"] = "这些设置对 CFG 模式和聊天模式都生效，下方可维护监听玩家列表。",
            ["general.auto_start_gsi"] = "启动时自动打开 GSI",
            ["general.start_gsi"] = "启动 GSI",
            ["general.stop_gsi"] = "停止 GSI",
            ["general.only_self_death"] = "仅在监听玩家列表死亡时触发",
            ["general.player_list_summary"] = "监听玩家列表 ({0})",
            ["general.player_add_placeholder"] = "添加监听玩家",
            ["general.player_list_empty"] = "暂无监听玩家，可以在上方添加",
            ["cfg.title"] = "CFG 模式",
            ["cfg.hint"] = "生成后会写入 autoexec，并使用当前全局/队内快捷键轮替发送语料。",
            ["cfg.cs2_path"] = "CS2 路径",
            ["cfg.apply_path"] = "应用路径",
            ["cfg.detect_path"] = "自动检测",
            ["cfg.generate"] = "生成 CFG",
            ["cfg.restore"] = "还原 CFG",
            ["cfg.prefer_team_chat"] = "优先使用队内聊天",
            ["cfg.global_keys"] = "全局聊天快捷键",
            ["cfg.team_keys"] = "队内聊天快捷键",
            ["cfg.current_global_keys"] = "当前全局快捷键: {0}",
            ["cfg.current_team_keys"] = "当前队内快捷键: {0}",
            ["cfg.key_placeholder"] = "例如 {0}",
            ["chat.title"] = "聊天模式",
            ["chat.hint"] = "聊天模式会模拟打开聊天框、粘贴消息并发送。",
            ["chat.key"] = "聊天按键",
            ["chat.delay"] = "按键延迟(ms)",
            ["chat.apply_key"] = "应用按键",
            ["chat.apply_delay"] = "应用延迟",
            ["chat.skip_window_check"] = "跳过 CS2 窗口检查",
            ["chat.force_mode"] = "强制连续打开聊天框",
            ["corpus.placeholder"] = "添加一条语料",
            ["corpus.empty"] = "暂无语料",
            ["corpus.import_file"] = "导入文件",
            ["corpus.export_file"] = "导出文件",
            ["corpus.import_url"] = "链接导入",
            ["corpus.url_placeholder"] = "粘贴纯文本或 JSON 语料链接",
            ["corpus.dialog.import_title"] = "选择语料文件",
            ["corpus.dialog.export_title"] = "导出语料文件",
            ["corpus.dialog.default_name"] = "Messages_export.txt",
            ["corpus.status.imported"] = "已导入语料 {0} 条，跳过 {1} 条",
            ["corpus.status.exported"] = "已导出语料 {0} 条到 {1}",
            ["corpus.error.invalid_url"] = "链接格式不正确",
            ["corpus.error.import_file_failed"] = "导入语料文件失败: {0}",
            ["corpus.error.import_url_failed"] = "从链接导入语料失败: {0}",
            ["corpus.error.export_failed"] = "导出语料失败: {0}",
            ["corpus.error.invalid_export_path"] = "导出路径无效",
            ["common.add"] = "添加",
            ["common.delete"] = "删除",
            ["gsi.initializing"] = "GSI初始化中",
            ["gsi.started"] = "GSI已启动",
            ["gsi.stopped"] = "GSI已停止",
            ["stats.summary"] = "消息数: {0}\n已生成CFG组数: {1}",
            ["status.init"] = "准备就绪",
            ["status.config_loaded"] = "设置已加载",
            ["status.player_dead"] = "检测到玩家死亡 {0}",
            ["status.message_sent"] = "已发送消息 {0}",
            ["status.message_added"] = "已添加语料 {0}",
            ["status.message_removed"] = "已删除语料 {0}",
            ["status.corpus_imported"] = "已导入语料 {0} 条，跳过 {1} 条",
            ["status.corpus_exported"] = "已导出语料 {0} 条到 {1}",
            ["status.config_saved"] = "设置已保存",
            ["status.cfg_failed"] = "CFG操作失败: {0}",
            ["cfg.status.detected_path"] = "检测到CS2游戏路径: {0}",
            ["cfg.status.path_not_found"] = "CS2路径未找到，请手动设置",
            ["cfg.status.created_cfg_dir"] = "创建CFG文件夹: {0}",
            ["cfg.status.set_path"] = "将CS2路径设置为: {0}",
            ["cfg.status.added_global_key"] = "已添加全局绑定快捷键: {0}",
            ["cfg.status.removed_global_key"] = "已删除全局快捷键: {0}",
            ["cfg.status.added_team_key"] = "已添加队内快捷键: {0}",
            ["cfg.status.removed_team_key"] = "已删除队内快捷键: {0}",
            ["cfg.status.autoexec_backup"] = "已创建 autoexec.cfg 备份",
            ["cfg.status.autoexec_updated"] = "更新 autoexec.cfg 完成",
            ["cfg.status.generated"] = "已生成 {0} 个CFG 文件 (全局+队内)",
            ["cfg.status.apply_done"] = "CFG生成完成，已写入 {0} 组消息绑定",
            ["cfg.status.restored"] = "已成功还原原CFG设置",
            ["cfg.status.no_backup"] = "没有备份文件用以还原",
            ["cfg.error.detect_path_failed"] = "检测CS2路径失败: {0}",
            ["cfg.error.create_cfg_dir_failed"] = "创建CFG文件夹失败: {0}",
            ["cfg.error.invalid_path"] = "非法路径",
            ["cfg.error.duplicate_global_key"] = "此全局快捷键已存在",
            ["cfg.error.invalid_key"] = "快捷键非法，请使用单个字母或数字作为快捷键",
            ["cfg.error.keep_one_global_key"] = "最少保留一个全局快捷键",
            ["cfg.error.global_key_not_found"] = "全局快捷键未找到",
            ["cfg.error.duplicate_team_key"] = "此队内快捷键已存在",
            ["cfg.error.keep_one_team_key"] = "最少保留一个队内快捷键",
            ["cfg.error.team_key_not_found"] = "队内快捷键未找到",
            ["cfg.error.empty_messages"] = "消息列表为空，请先添加一条消息",
            ["cfg.error.invalid_cs2_path"] = "CS2游戏路径不存在或路径非法",
            ["cfg.error.invalid_cfg_dir"] = "CFG文件夹不存在或不能创建",
            ["cfg.error.generate_failed"] = "生成CFG时出错: {0}",
            ["cfg.error.update_autoexec_failed"] = "更新 autoexec.cfg 时出错: {0}",
            ["cfg.error.invalid_cfg_path"] = "CFG路径不存在或路径非法",
            ["cfg.error.count_cfg_failed"] = "统计CFG文件数量时出错: {0}",
            ["cfg.error.restore_failed"] = "还原CFG时出错: {0}",
            ["cfg.error.restore_invalid_path"] = "CFG 路径无效或未设置"
        },
        ["zh-TW"] = new()
        {
            ["language.label"] = "介面語言",
            ["language.zh-CN"] = "簡體中文",
            ["language.zh-TW"] = "繁體中文",
            ["mode.cfg"] = "CFG模式",
            ["mode.chat"] = "聊天模式",
            ["mode.cfg_description"] = "生成遊戲設定檔，偵測到死亡後自動按下綁定快捷鍵發送訊息",
            ["mode.chat_description"] = "偵測到死亡後自動模擬按鍵發送訊息",
            ["section.general"] = "通用設定",
            ["section.cfg"] = "CFG 模式設定",
            ["section.chat"] = "聊天模式設定",
            ["section.corpus"] = "語料管理",
            ["general.title"] = "通用設定",
            ["general.hint"] = "這些設定對 CFG 模式和聊天模式都生效，下方可維護監聽玩家列表。",
            ["general.auto_start_gsi"] = "啟動時自動開啟 GSI",
            ["general.start_gsi"] = "啟動 GSI",
            ["general.stop_gsi"] = "停止 GSI",
            ["general.only_self_death"] = "僅在監聽玩家列表死亡時觸發",
            ["general.player_list_summary"] = "監聽玩家列表 ({0})",
            ["general.player_add_placeholder"] = "新增監聽玩家",
            ["general.player_list_empty"] = "目前沒有監聽玩家，可在上方新增",
            ["cfg.title"] = "CFG 模式",
            ["cfg.hint"] = "生成後會寫入 autoexec，並使用目前的全域/隊內快捷鍵輪替發送語料。",
            ["cfg.cs2_path"] = "CS2 路徑",
            ["cfg.apply_path"] = "套用路徑",
            ["cfg.detect_path"] = "自動偵測",
            ["cfg.generate"] = "生成 CFG",
            ["cfg.restore"] = "還原 CFG",
            ["cfg.prefer_team_chat"] = "優先使用隊內聊天",
            ["cfg.global_keys"] = "全域聊天快捷鍵",
            ["cfg.team_keys"] = "隊內聊天快捷鍵",
            ["cfg.current_global_keys"] = "目前全域快捷鍵: {0}",
            ["cfg.current_team_keys"] = "目前隊內快捷鍵: {0}",
            ["cfg.key_placeholder"] = "例如 {0}",
            ["chat.title"] = "聊天模式",
            ["chat.hint"] = "聊天模式會模擬開啟聊天框、貼上訊息並發送。",
            ["chat.key"] = "聊天按鍵",
            ["chat.delay"] = "按鍵延遲(ms)",
            ["chat.apply_key"] = "套用按鍵",
            ["chat.apply_delay"] = "套用延遲",
            ["chat.skip_window_check"] = "跳過 CS2 視窗檢查",
            ["chat.force_mode"] = "強制連續開啟聊天框",
            ["corpus.placeholder"] = "新增一條語料",
            ["corpus.empty"] = "目前沒有語料",
            ["corpus.import_file"] = "匯入檔案",
            ["corpus.export_file"] = "匯出檔案",
            ["corpus.import_url"] = "連結匯入",
            ["corpus.url_placeholder"] = "貼上純文字或 JSON 語料連結",
            ["corpus.dialog.import_title"] = "選擇語料檔案",
            ["corpus.dialog.export_title"] = "匯出語料檔案",
            ["corpus.dialog.default_name"] = "Messages_export.txt",
            ["corpus.status.imported"] = "已匯入語料 {0} 條，跳過 {1} 條",
            ["corpus.status.exported"] = "已匯出語料 {0} 條到 {1}",
            ["corpus.error.invalid_url"] = "連結格式不正確",
            ["corpus.error.import_file_failed"] = "匯入語料檔案失敗: {0}",
            ["corpus.error.import_url_failed"] = "從連結匯入語料失敗: {0}",
            ["corpus.error.export_failed"] = "匯出語料失敗: {0}",
            ["corpus.error.invalid_export_path"] = "匯出路徑無效",
            ["common.add"] = "新增",
            ["common.delete"] = "刪除",
            ["gsi.initializing"] = "GSI初始化中",
            ["gsi.started"] = "GSI已啟動",
            ["gsi.stopped"] = "GSI已停止",
            ["stats.summary"] = "訊息數: {0}\n已生成CFG組數: {1}",
            ["status.init"] = "準備就緒",
            ["status.config_loaded"] = "設定已載入",
            ["status.player_dead"] = "偵測到玩家死亡 {0}",
            ["status.message_sent"] = "已發送訊息 {0}",
            ["status.message_added"] = "已新增語料 {0}",
            ["status.message_removed"] = "已刪除語料 {0}",
            ["status.corpus_imported"] = "已匯入語料 {0} 條，跳過 {1} 條",
            ["status.corpus_exported"] = "已匯出語料 {0} 條到 {1}",
            ["status.config_saved"] = "設定已儲存",
            ["status.cfg_failed"] = "CFG操作失敗: {0}",
            ["cfg.status.detected_path"] = "偵測到CS2遊戲路徑: {0}",
            ["cfg.status.path_not_found"] = "找不到CS2路徑，請手動設定",
            ["cfg.status.created_cfg_dir"] = "已建立CFG資料夾: {0}",
            ["cfg.status.set_path"] = "已將CS2路徑設定為: {0}",
            ["cfg.status.added_global_key"] = "已新增全域綁定快捷鍵: {0}",
            ["cfg.status.removed_global_key"] = "已刪除全域快捷鍵: {0}",
            ["cfg.status.added_team_key"] = "已新增隊內快捷鍵: {0}",
            ["cfg.status.removed_team_key"] = "已刪除隊內快捷鍵: {0}",
            ["cfg.status.autoexec_backup"] = "已建立 autoexec.cfg 備份",
            ["cfg.status.autoexec_updated"] = "更新 autoexec.cfg 完成",
            ["cfg.status.generated"] = "已生成 {0} 個CFG 檔案 (全域+隊內)",
            ["cfg.status.apply_done"] = "CFG生成完成，已寫入 {0} 組訊息綁定",
            ["cfg.status.restored"] = "已成功還原原CFG設定",
            ["cfg.status.no_backup"] = "沒有備份檔可供還原",
            ["cfg.error.detect_path_failed"] = "偵測CS2路徑失敗: {0}",
            ["cfg.error.create_cfg_dir_failed"] = "建立CFG資料夾失敗: {0}",
            ["cfg.error.invalid_path"] = "非法路徑",
            ["cfg.error.duplicate_global_key"] = "此全域快捷鍵已存在",
            ["cfg.error.invalid_key"] = "快捷鍵非法，請使用單個字母或數字作為快捷鍵",
            ["cfg.error.keep_one_global_key"] = "至少保留一個全域快捷鍵",
            ["cfg.error.global_key_not_found"] = "找不到全域快捷鍵",
            ["cfg.error.duplicate_team_key"] = "此隊內快捷鍵已存在",
            ["cfg.error.keep_one_team_key"] = "至少保留一個隊內快捷鍵",
            ["cfg.error.team_key_not_found"] = "找不到隊內快捷鍵",
            ["cfg.error.empty_messages"] = "訊息列表為空，請先新增一條訊息",
            ["cfg.error.invalid_cs2_path"] = "CS2遊戲路徑不存在或路徑非法",
            ["cfg.error.invalid_cfg_dir"] = "CFG資料夾不存在或無法建立",
            ["cfg.error.generate_failed"] = "生成CFG時出錯: {0}",
            ["cfg.error.update_autoexec_failed"] = "更新 autoexec.cfg 時出錯: {0}",
            ["cfg.error.invalid_cfg_path"] = "CFG路徑不存在或路徑非法",
            ["cfg.error.count_cfg_failed"] = "統計CFG檔案數量時出錯: {0}",
            ["cfg.error.restore_failed"] = "還原CFG時出錯: {0}",
            ["cfg.error.restore_invalid_path"] = "CFG 路徑無效或尚未設定"
        }
    };

    public static string CurrentLanguage { get; private set; } = DefaultLanguage;

    public static void SetLanguage(string? language, bool notify = true)
    {
        var normalized = NormalizeLanguage(language);
        var changed = !string.Equals(CurrentLanguage, normalized, StringComparison.OrdinalIgnoreCase);
        CurrentLanguage = normalized;
        if (changed && notify)
        {
            EventBus.TriggerEvent(new LanguageChangedEvent { Language = CurrentLanguage });
        }
    }

    public static string NormalizeLanguage(string? language)
    {
        return language?.ToLowerInvariant() switch
        {
            "zh-tw" or "zh-hk" or "zh-mo" => TraditionalChinese,
            _ => DefaultLanguage
        };
    }

    public static string T(string key, params object[] args)
    {
        var table = Translations.TryGetValue(CurrentLanguage, out var currentTable)
            ? currentTable
            : Translations[DefaultLanguage];

        var template = table.TryGetValue(key, out var value)
            ? value
            : Translations[DefaultLanguage].GetValueOrDefault(key, key);

        return args.Length == 0 ? template : string.Format(template, args);
    }
}
