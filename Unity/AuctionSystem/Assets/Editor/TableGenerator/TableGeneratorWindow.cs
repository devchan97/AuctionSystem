using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AuctionSystem.Editor.TableGenerator
{
    public class TableGeneratorWindow : EditorWindow
    {
        private SupabaseTableConfig _config;
        private Vector2 _scroll;

        private enum TableStatus { Unknown, Exists, Missing, Checking }
        private Dictionary<string, TableStatus> _tableStatus = new();
        private bool _isChecking;
        private string _lastError;

        [MenuItem("Tools/Auction/Table Generator")]
        public static void Open()
        {
            var win = GetWindow<TableGeneratorWindow>("Supabase Table Generator");
            win.minSize = new Vector2(480, 560);
            win.Show();
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawConfigSection();
            EditorGUILayout.Space(8);
            DrawTableStatusSection();
            EditorGUILayout.Space(8);
            DrawSqlSection();
            EditorGUILayout.Space(8);
            DrawExportSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigSection()
        {
            EditorGUILayout.LabelField("1. 연결 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _config = (SupabaseTableConfig)EditorGUILayout.ObjectField(
                "Config Asset", _config, typeof(SupabaseTableConfig), false);

            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "SupabaseTableConfig asset을 할당하거나 아래 버튼으로 생성하세요.\n" +
                    "생성된 asset에 Supabase URL과 Service Role Key를 입력하세요.",
                    MessageType.Info);

                if (GUILayout.Button("Config Asset 생성"))
                    CreateConfigAsset();
            }
            else if (!_config.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "supabaseUrl과 serviceRoleKey를 입력하세요.\n" +
                    "Service Role Key는 Supabase Dashboard > Settings > API 에서 확인.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("연결 설정 완료.", MessageType.None);
            }

            EditorGUI.indentLevel--;
        }

        private void CreateConfigAsset()
        {
            string dir = "Assets/Editor/TableGenerator";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = $"{dir}/SupabaseTableConfig.asset";
            if (File.Exists(path))
            {
                _config = AssetDatabase.LoadAssetAtPath<SupabaseTableConfig>(path);
                return;
            }

            var asset = CreateInstance<SupabaseTableConfig>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _config = asset;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void DrawTableStatusSection()
        {
            EditorGUILayout.LabelField("2. 테이블 상태 확인", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(_config == null || !_config.IsValid || _isChecking))
            {
                if (GUILayout.Button(_isChecking ? "확인 중..." : "모든 테이블 상태 확인"))
                    CheckAllTables();
            }

            if (!string.IsNullOrEmpty(_lastError))
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);

            foreach (var table in SqlTemplates.All)
            {
                _tableStatus.TryGetValue(table.Name, out var status);
                DrawTableRow(table.Name, status);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawTableRow(string tableName, TableStatus status)
        {
            var color = status switch
            {
                TableStatus.Exists   => new Color(0.3f, 0.8f, 0.3f),
                TableStatus.Missing  => new Color(0.9f, 0.4f, 0.3f),
                TableStatus.Checking => Color.yellow,
                _                    => Color.gray
            };
            var label = status switch
            {
                TableStatus.Exists   => "✓ 존재",
                TableStatus.Missing  => "✗ 없음",
                TableStatus.Checking => "확인 중...",
                _                    => "미확인"
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(tableName, GUILayout.Width(160));
                var prev = GUI.color;
                GUI.color = color;
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                GUI.color = prev;
            }
        }

        private void DrawSqlSection()
        {
            EditorGUILayout.LabelField("3. SQL 생성 및 복사", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (var table in SqlTemplates.All)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool isMissing = _tableStatus.TryGetValue(table.Name, out var s) && s == TableStatus.Missing;

                    var style = new GUIStyle(GUI.skin.button);
                    if (isMissing)
                    {
                        style.normal.textColor = new Color(1f, 0.6f, 0.3f);
                        style.fontStyle = FontStyle.Bold;
                    }

                    if (GUILayout.Button($"{table.Name} SQL 복사", style))
                    {
                        GUIUtility.systemCopyBuffer = table.Sql;
                        ShowNotification(new GUIContent($"{table.Name} SQL 복사됨!"));
                    }
                }
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("전체 SQL 한 번에 복사 (마이그레이션용)"))
            {
                var sb = new StringBuilder();
                foreach (var t in SqlTemplates.All)
                    sb.AppendLine(t.Sql).AppendLine();
                GUIUtility.systemCopyBuffer = sb.ToString();
                ShowNotification(new GUIContent("전체 SQL 복사됨!"));
            }

            EditorGUI.indentLevel--;
        }

        private void DrawExportSection()
        {
            EditorGUILayout.LabelField("4. SQL 파일 내보내기", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (GUILayout.Button("전체 SQL → migration 파일로 내보내기"))
                ExportToMigrationFile();

            EditorGUI.indentLevel--;
        }

        private void ExportToMigrationFile()
        {
            string defaultPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../../../../../supabase/migrations"));

            string savePath = EditorUtility.SaveFilePanel(
                "SQL 파일 저장", defaultPath, "00000_initial_schema", "sql");

            if (string.IsNullOrEmpty(savePath)) return;

            var sb = new StringBuilder();
            sb.AppendLine("-- 경매장 시스템 초기 스키마");
            sb.AppendLine("-- 생성: Unity TableGenerator");
            sb.AppendLine();
            foreach (var t in SqlTemplates.All)
                sb.AppendLine(t.Sql).AppendLine();

            File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
            EditorUtility.RevealInFinder(savePath);
            ShowNotification(new GUIContent("SQL 파일 내보내기 완료!"));
        }

        private void CheckAllTables()
        {
            _lastError = null;
            foreach (var t in SqlTemplates.All)
                _tableStatus[t.Name] = TableStatus.Checking;

            _isChecking = true;
            EditorApplication.update += PollRequests;

            _pendingRequests = new List<(string name, UnityWebRequest req)>();
            foreach (var table in SqlTemplates.All)
            {
                string url = $"{_config.supabaseUrl}/rest/v1/{table.Name}?limit=0";
                var req = UnityWebRequest.Get(url);
                req.SetRequestHeader("apikey", _config.serviceRoleKey);
                req.SetRequestHeader("Authorization", "Bearer " + _config.serviceRoleKey);
                req.SendWebRequest();
                _pendingRequests.Add((table.Name, req));
            }
        }

        private List<(string name, UnityWebRequest req)> _pendingRequests;

        private void PollRequests()
        {
            if (_pendingRequests == null) return;

            bool allDone = true;
            foreach (var (name, req) in _pendingRequests)
            {
                if (!req.isDone) { allDone = false; continue; }

                if (_tableStatus[name] == TableStatus.Checking)
                {
                    if (req.responseCode == 200)
                        _tableStatus[name] = TableStatus.Exists;
                    else if (req.responseCode == 404 || req.downloadHandler.text.Contains("does not exist"))
                        _tableStatus[name] = TableStatus.Missing;
                    else if (req.result == UnityWebRequest.Result.ConnectionError)
                        _lastError = "연결 오류: URL 또는 Key를 확인하세요.";
                    else
                        _tableStatus[name] = TableStatus.Missing;
                }
            }

            if (allDone)
            {
                _isChecking = false;
                EditorApplication.update -= PollRequests;
                _pendingRequests = null;
                Repaint();
            }
            else
            {
                Repaint();
            }
        }

        void OnDisable()
        {
            EditorApplication.update -= PollRequests;
        }
    }
}
