/*
 * vsqx2ccs
 *  あかつきみさき(みくちぃP)
 * 
 * ***重要-免責事項-***
 *  本ツールを実行したことによって損害・不利益・事故等が発生した場合も一切の責任を負いません.
 * 
 * このツールについて
 *  VOCALOID3のvsqxデータを,CeVIOのCreative Studio Freeのボーカルトラックデータのccsに変換します.
 * 
 * 使用方法
 *  対象のvsqxファイルをexeにドラック&ドロップしてください.
 *  同じフォルダ内部に「vsqxのベースネーム.ccs」が生成されます.
 *  (複数同時に処理可能,vsqx以外は無視されます.)
 * 
 * 動作環境
 *  Microsoft .NET Framework 4.5(たぶん)
 *  VOCALOID3かVOCALOID3 Tinyが標準インストール先にインストールされている環境
 * 
 * 既知の未実装項目
 *  ・VOCALOID4以降への対応
 *  ・CeVIO製品版への対応確認
 *  ・テンポチェンジ
 *  ・拍子チェンジ
 *  ・トラックのミュート情報
 *  ・トラックのソロ情報
 * 
 * ライセンス
 *  MIT License
 * 
 * バージョン情報
 *  2016/11/01 Ver 0.1.2 Update
 *      ドキュメント修正.
 * 
 *  2016/11/01 Ver 0.1.1 Update
 *      ドキュメントに追記.
 * 
 *  2013/06/29 Ver 0.1.0 Update
 *      複数トラックの対応.
 *      識別用GUIDを生成するようにした.
 *      vsqx2ccsと同じフォルダーにust2ccsが存在する場合,ustファイルをust2ccsに渡すようにした.
 *   
 *  2013/06/16 Ver 0.0.2 Update
 *      ベースネーム.vsqx.○○の時に読み込まれてしまう問題の修正.
 *      いくつかの問題の修正.
 *      xsdファイルがなくても実行できるようにした(簡易的処理).
 *  
 *  2013/06/16 Ver 0.0.1 Release
*/

/* TODO
 * 通常実行時の「ファイルを開く」のダイアログ
 * ust2ccsがこのツールと同じフォルダ内にある時,渡されたustファイルをust2ccsに引き渡すようにする
 * VSQXのxsd判定
 * オクターブチェンジの実装
 * */



using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Windows.Forms;

namespace vsqx2ccs
{
    class BaseTick
    {
        private const int VOCALOID3_TIME_RESOLUTION = 480;
        private const int CEVIO_TIME_RESOLUTION     = 960;
        private const int START_TICK                = 3840;
        private const int SHIFT_OCTAVE              = -1;
        private const int CENTER_NOTE_NUMBER        = 64;

        private const string CCS_CODE = "7251BC4B6168E7B2992FA620BD3E1E77";

        public int GetV3Tick()
        {
            return VOCALOID3_TIME_RESOLUTION;
        }

        public int GetCeVIOTick()
        {
            return CEVIO_TIME_RESOLUTION;
        }

        public int GetDifference()
        {
            return CEVIO_TIME_RESOLUTION / VOCALOID3_TIME_RESOLUTION;
        }

        public int GetTimeRes()
        {
            return CEVIO_TIME_RESOLUTION;
        }

        public int GetStartTick()
        {
            return START_TICK;
        }

        public int GetShiftOctave()
        {
            return SHIFT_OCTAVE;
        }

        public int GetCenterNoteNum()
        {
            return CENTER_NOTE_NUMBER;
        }

        public string GetCode()
        {
            return CCS_CODE;
        }
    }

    class BpmData
    {
        private string tempo;
        private int clock;
        private string integerPoint;
        private string decimalPoint;

        private BaseTick deff = new BaseTick();

        public void SetTempo(string currentBpm)
        {
            tempo = currentBpm;
        }

        public void SetClock(string currentClock)
        {
            clock = int.Parse(currentClock) * deff.GetDifference() / 2 + deff.GetStartTick();
        }

        public void SetIntegerPoint(string currentInteger)
        {
            integerPoint = currentInteger;
        }

        public void SetDecimalPoint(string currentDecimalPoint)
        {
            decimalPoint = currentDecimalPoint;
        }

        public string GetTempo()
        {
            return tempo;
        }

        public string GetClock()
        {
            return clock.ToString();
        }

        public string GetIntegerPoint()
        {
            return integerPoint;
        }

        public string GetDecimalPoint()
        {
            return decimalPoint;
        }
    }

    class BeatData
    {
        private string denomi;  // 分母
        private string nume;    // 分子
        private int posMes;     // テンポチェンジポイント

        private BaseTick deff = new BaseTick();

        public void SetDenomi(string targetDenomi)
        {
            denomi = targetDenomi;
        }

        public string GetDenomi()
        {
            return denomi;
        }

        public void SetNume(string targetNume)
        {
            nume = targetNume;
        }

        public string GetNume()
        {
            return nume;
        }

        public void SetPosMes(int currentPosMes)
        {
            //Console.WriteLine(currentPosMes + "\n" + int.Parse(nume) + "\n" + int.Parse(denomi));
            //posMes = (currentPosMes - int.Parse(nume) / int.Parse(denomi)) * deff.GetStartTick();
            posMes = currentPosMes * deff.GetStartTick() * 4 / 4;// *deff.GetV3Tick();// +deff.GetStartTick();
        }

        public int GetPosMes()
        {
            return posMes;
        }
    }

    class TrackData
    {
        private string trackName;
        private List<NoteData> note;

        private int trackNum;

        private int mute;
        private int solo;
        private int volume;

        // トラックとミキサー識別用のGUID
        private Guid guidValue;

        // インスタンス作成時にトラックとミキサーの識別用GUIDを作成する
        public TrackData()
        {
            NoteData note = new NoteData();
            guidValue = Guid.NewGuid();
        }

        public string GetTrackGuid()
        {
            // ハイフンを追加したGUIDを返す
            return guidValue.ToString("D");
        }

        public void SetTrackName(string name)
        {
            trackName = name;
        }

        public string GetTrackName()
        {
            return trackName;
        }

        public void SetMute(int trackMute)
        {
            mute = trackMute;
        }

        public int GetMute()
        {
            return mute;
        }

        public void SetSolo(int trackSolo)
        {
            solo = trackSolo;
        }

        public int GetSolo()
        {
            return solo;
        }

        public void SetVolume(int trackVol)
        {
            volume = trackVol;
        }

        public int GetVolume()
        {
            return volume;
        }

        public void SetTrackNum(string track)
        {
            trackNum = int.Parse(track);
        }

        public int GetTrackNum()
        {
            return trackNum;
        }

        public void SetNote(List<NoteData> currentNote)
        {
            note = currentNote;
        }

        public List<NoteData> GetNote()
        {
            return note;
        }
    }

    class NoteData : IEnumerable
    {
        // ノートナンバーの範囲
        private const int NOTENUM_MAX   = 127;
        private const int NOTENUM_MIN   = 0;
        private const int DODECAPHONISM = 12;

        // ノートナンバー・歌詞
        private int noteNum;
        private string lyric;

        // ノートの開始位置・デュレーション
        private int position1;
        private int position2;
        private string beginPos;
        private string duration;
        private TimeSpan playTime;

        // CeVIOでのノートナンバー
        private int pitchStep;
        private int pitchOctave;

        private BaseTick deff = new BaseTick();

        // 初期設定
        public NoteData()
        {
            noteNum     = deff.GetCenterNoteNum();
            lyric       = "a";
            beginPos    = "0";
            duration    = deff.GetCeVIOTick().ToString();
            pitchStep   = 5;
            pitchOctave = 0;
        }

        // 歌詞をセット
        public void SetNoteLyric(string vsqxLyric)
        {
            lyric = vsqxLyric;
        }

        // ノートナンバーをセット
        public void SetNoteNum(int vsqxNoteNum)
        {
            noteNum = vsqxNoteNum;
            SetTick(noteNum);
        }

        // ノートの開始位置をセット
        public void SetBeginTick(string vsqxBegin)
        {
            // CeVIOでの処理のため2倍 + 開始値
            position1 = int.Parse(vsqxBegin) * deff.GetDifference() + deff.GetStartTick();
            beginPos  = position1.ToString();
        }

        // ノートのデュレーションをセット
        public void SetDuration(string vsqxDuration)
        {
            // CeVIOでの処理のため補正
            position2 = int.Parse(vsqxDuration) * deff.GetDifference();
            duration  = position2.ToString();
        }

        // ノートナンバーからCeVIOでの音階をセットする
        public void SetTick(int noteNum)
        {
            // ノートナンバーが範囲外なら範囲内部にする
            if (noteNum > NOTENUM_MAX) noteNum = NOTENUM_MAX;
            if (noteNum < NOTENUM_MIN) noteNum = NOTENUM_MIN;

            // 音階調整
            pitchOctave = noteNum / DODECAPHONISM + deff.GetShiftOctave();
            pitchStep   = noteNum % DODECAPHONISM;
        }

        public string GetLyric()
        {
            return lyric;
        }

        public string GetNoteNum()
        {
            return noteNum.ToString();
        }

        public string GetClock()
        {
            return beginPos.ToString();
        }

        public string GetDuration()
        {
            return duration.ToString();
        }

        public string GetPitchOctave()
        {
            return pitchOctave.ToString();
        }

        public string GetPitchStep()
        {
            return pitchStep.ToString();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void SetPlayTime(string currentPartTick)
        {
            playTime = new TimeSpan(int.Parse(currentPartTick));
        }

        public string GetPlayTime()
        {
            Console.WriteLine(playTime.TotalHours.ToString() + ":" + playTime.TotalMinutes.ToString() + ":" + playTime.TotalSeconds.ToString());
            return playTime.TotalHours.ToString() + ":" + playTime.TotalMinutes.ToString() + ":" + playTime.TotalSeconds.ToString();
        }
    }

    class SchemaCheck
    {
        public static void XMLSchemaCheck(string vsqxXsd, string currentFileName)
        {
            XmlSchema schema = null;
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.Add("http://www.yamaha.co.jp/vocaloid/schema/vsq3/", vsqxXsd);

            foreach (XmlSchema s in schemaSet.Schemas())
            {
                schema = s;
            }

            XmlDocument xdoc = new XmlDocument();
            xdoc.Schemas.Add(schema);
            xdoc.Load(currentFileName);
            xdoc.Validate(ValidationCallBack);
        }

        private static void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Error)
            {
                //return false;
                Console.WriteLine("Error: " + args.Message);
            }
            //return true;
        }
    }

    class vsqx2ccs
    {
        static void Main(string[] vsqxFiles)
        {
            string[] baseName = { };

            string saveFileName     = "";

            const int VS_TRACK_MAX = 16;

            List<TrackData> trackArray = new List<TrackData>();
            List<NoteData>[] noteArray = new List<NoteData>[VS_TRACK_MAX];
            List<BpmData> bpmArray     = new List<BpmData>();
            List<BeatData> beatArray   = new List<BeatData>();
            BaseTick deff = new BaseTick();

            ArrayList lyricArray    = new ArrayList();
            ArrayList posTickArray  = new ArrayList();
            ArrayList durationArray = new ArrayList();

            string vsqxXsd = @"C:\Program Files (x86)\VOCALOID3\vsq3.xsd";
            string ust2ccs = System.IO.Directory.GetCurrentDirectory() + @"\ust2ccs.exe";

            if (System.Environment.Is64BitOperatingSystem)
            {
                vsqxXsd = @"C:\Program Files (x86)\VOCALOID3\vsq3.xsd";
                Console.WriteLine("OS : 64bit");
                // フォルダ (ディレクトリ) が存在しているかどうか確認する
                if (System.IO.File.Exists(vsqxXsd))
                {
                    Console.WriteLine("VOCALOID3");
                }
                else
                {
                    vsqxXsd = @"C:\Program Files (x86)\VOCALOID3TINY\vsq3.xsd";
                    Console.WriteLine("VOCALOID3 TINY");
                }
            }
            else
            {
                vsqxXsd = @"C:\Program Files\VOCALOID3\vsq3.xsd";
                Console.WriteLine("OS : 32bit");
                // フォルダ (ディレクトリ) が存在しているかどうか確認する
                if (System.IO.File.Exists(vsqxXsd))
                {
                    Console.WriteLine("VOCALOID3");
                }
                else
                {
                    vsqxXsd = @"C:\Program Files\VOCALOID3TINY\vsq3.xsd";
                    Console.WriteLine("VOCALOID3 TINY");
                }
            }

            // vsqxのスキーマファイルの確認
            string[] xsdBasename = vsqxXsd.Split('\\');

            Console.Write("Check vsqx xsd file -> ");

            if (!System.IO.File.Exists(vsqxXsd))
            {
                Console.WriteLine("NO FILES");
                Console.WriteLine(xsdBasename[xsdBasename.Length - 1] + " Not Found.");
                Console.WriteLine("Push Enter key.");
                Console.ReadLine();

                /*
                 * // とりあえず今はなしでもOK
                return;
                 */
            }

            Console.WriteLine("OK");


            Console.Write("Check ust2ccs -> ");
            if (System.IO.File.Exists(ust2ccs))
            {
                Console.WriteLine("OK\n");
            }
            else
            {
                Console.WriteLine("Not Found.\n");
            }

            /*
            if (vsqxFiles.Length <= 0)
            {
                OpenFileDialog openVsqxFileDialog = new OpenFileDialog();
                // ダイアログのタイトルを設定する
                openVsqxFileDialog.Title = "ファイルを開く";
                // 初期表示するディレクトリを設定する
                openVsqxFileDialog.InitialDirectory = @"C:\";
                // 初期表示するファイル名を設定する
                openVsqxFileDialog.FileName = "";
                // ファイルのフィルタを設定する
                openVsqxFileDialog.Filter = "VOCALOID3 Sequence|*.vsqx|すべてのファイル|*.*";
                // ファイルの種類 の初期設定を 2 番目に設定する (初期値 1)
                openVsqxFileDialog.FilterIndex = 1;
                // ダイアログボックスを閉じる前に現在のディレクトリを復元する (初期値 false)
                // openVsqxFileDialog.RestoreDirectory = true;
                // 複数のファイルを選択可能にする (初期値 false)
                openVsqxFileDialog.Multiselect = true;
                // [ヘルプ] ボタンを表示する (初期値 false)
                openVsqxFileDialog.ShowHelp = false;
                // [読み取り専用] チェックボックスを表示する (初期値 false)
                // openVsqxFileDialog.ShowReadOnly = true;
                // [読み取り専用] チェックボックスをオンにする (初期値 false)
                // openVsqxFileDialog.ReadOnlyChecked = true;
                // 存在しないファイルを指定した場合は警告を表示する (初期値 true)
                //openVsqxFileDialog.CheckFileExists = true;
                // 存在しないパスを指定した場合は警告を表示する (初期値 true)
                //openVsqxFileDialog.CheckPathExists = true;
                // 拡張子を指定しない場合は自動的に拡張子を付加する (初期値 true)
                openVsqxFileDialog.AddExtension = true;
                // 有効な Win32 ファイル名だけを受け入れるようにする (初期値 true)
                //openVsqxFileDialog.ValidateNames = true;
                // ダイアログを表示し、戻り値が [OK] の場合は、選択したファイルを表示する

                if (openVsqxFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Console.WriteLine(openVsqxFileDialog.FileName);
                    vsqxFiles = openVsqxFileDialog.FileNames;
                }

                // 不要になった時点で破棄する (正しくは オブジェクトの破棄を保証する を参照)
                openVsqxFileDialog.Dispose();
            }
            */


            foreach (String currentFileName in vsqxFiles)
            {
                baseName = currentFileName.Split('.');

                if (baseName[baseName.Length - 1] != "vsqx")
                {
                    Console.WriteLine(currentFileName + "\nNot vsqx.\nSkip\n");
                    continue;
                }


                /*
                // vsqxのみ処理
                if (baseName[baseName.Length - 1] == "ust" && System.IO.File.Exists(ust2ccs))
                {
                    Console.Write(currentFileName + "\nSend to ust2ccs.");
                    // TODO:センド処理
                    System.Diagnostics.Process pro = new System.Diagnostics.Process();

                    pro.StartInfo.FileName               = "cmd.exe";                  // コマンド名
                    pro.StartInfo.Arguments              = "/c " + ust2ccs + currentFileName;    // 引数
                    pro.StartInfo.CreateNoWindow         = true;                     // DOSプロンプトの黒い画面を非表示
                    pro.StartInfo.UseShellExecute        = false;                    // プロセスを新しいウィンドウで起動するか否か
                    pro.StartInfo.RedirectStandardOutput = true;                     // 標準出力をリダイレクトして取得したい

                    pro.Start();

                    //プロセス終了まで待機する
                    //WaitForExitはReadToEndの後である必要がある
                    //(親プロセス、子プロセスでブロック防止のため)
                    pro.WaitForExit();
                    pro.Close();

                    Console.WriteLine("\n->\n" + baseName[0] + ".ccs\n");
                    continue;
                }
                else if (baseName[baseName.Length - 1] != "vsqx")
                {
                    Console.WriteLine(currentFileName + "\nNot vsqx.\nSkip\n");
                    continue;
                }

                // TODO: vsqx判定処理
                if (baseName[baseName.Length - 1] != "vsqx")
                {
                    //XMLSchemaCheck(vsqxXsd, currentFileName);
                    Console.WriteLine(currentFileName + "\nBroken vsqx.\nSkip\n");
                    continue;
                }
                */
                
                // すでにファイルがある場合の処理
                /*
                int i = 0;
                while (!System.IO.File.Exists(baseName[0] + ".ccs"))
                {
                    baseName[0] = baseName[0] + " (" + i + ")";
                    i++;
                }
                */

                saveFileName = baseName[0] + ".ccs";

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent            = true;                                // インデントあり
                settings.Encoding          = Encoding.UTF8;

                XmlWriter writer = null;
                XmlReader reader = null;
                NoteData note    = new NoteData();


                try
                {
                    reader = XmlReader.Create(new StreamReader(currentFileName));

                    while (reader.Read())   // ノードを一つずつ読む
                    {
                        if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "vsUnit")
                        {
                            while (reader.Read())//vsUnit
                            {
                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "vsTrackNo")
                                {
                                    // トラック作成
                                    trackArray.Add(new TrackData());
                                    trackArray[trackArray.Count - 1].SetTrackNum(reader.ReadElementString());
                                }

                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "mute")
                                {
                                    trackArray[trackArray.Count - 1].SetMute(int.Parse(reader.ReadElementString()));
                                }

                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "solo")
                                {
                                    trackArray[trackArray.Count - 1].SetSolo(int.Parse(reader.ReadElementString()));
                                    break;
                                }
                            }
                        }// トラック if end


                        // 拍子取得
                        if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.LocalName == "timeSig")
                        {
                            while (reader.Read())//tempo
                            {
                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "posMes")
                                {
                                    beatArray.Add(new BeatData());
                                    beatArray[beatArray.Count - 1].SetPosMes(int.Parse(reader.ReadElementString()));
                                }
                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "nume")
                                {
                                    beatArray[beatArray.Count - 1].SetNume(reader.ReadElementString());
                                }
                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "denomi")
                                {
                                    beatArray[beatArray.Count - 1].SetDenomi(reader.ReadElementString());
                                    break;
                                }
                            }//while timeSig end
                        }//if timeSig end


                        // テンポ取得
                        if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.LocalName == "tempo")
                        {
                            while (reader.Read())//tempo
                            {
                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.LocalName == "posTick")
                                {
                                    bpmArray.Add(new BpmData());
                                    bpmArray[bpmArray.Count - 1].SetClock(reader.ReadElementString());
                                }

                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.LocalName == "bpm")
                                {
                                    bpmArray[bpmArray.Count - 1].SetTempo(reader.ReadElementString());

                                    // テンポが8000/12000といった風に長さが違う場合の処理,V3上でのテンポは20~300なのでxsd判定すればOK
                                    if (bpmArray[bpmArray.Count - 1].GetTempo().Length > 4)
                                    {
                                        bpmArray[bpmArray.Count - 1].SetIntegerPoint(bpmArray[bpmArray.Count - 1].GetTempo().Substring(0, 3));
                                        bpmArray[bpmArray.Count - 1].SetDecimalPoint(bpmArray[bpmArray.Count - 1].GetTempo().Substring(3));
                                    }
                                    else
                                    {
                                        bpmArray[bpmArray.Count - 1].SetIntegerPoint(bpmArray[bpmArray.Count - 1].GetTempo().Substring(0, 2));
                                        bpmArray[bpmArray.Count - 1].SetDecimalPoint(bpmArray[bpmArray.Count - 1].GetTempo().Substring(2));
                                    }
                                    break;
                                }
                            }//while temp end
                        }//if temp end

                        

                        // トラックに含まれるノート作成
                        if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.LocalName == "vsTrack")
                        {
                            int TrackNo = 0;
                            while (reader.Read())//vsTrack
                            {

                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "vsTrackNo")
                                {
                                    noteArray[TrackNo] = new List<NoteData>();
                                    trackArray[TrackNo].SetTrackNum(reader.ReadElementString());
                                }

                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "trackName")
                                {
                                    trackArray[TrackNo].SetTrackName(reader.ReadElementString());
                                }

                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "musicalPart")
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "note")
                                        {
                                            while (reader.Read())
                                            {
                                                // 開始地点
                                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "posTick")
                                                {
                                                    // ノートごとのクラスの作成
                                                    noteArray[TrackNo].Add(new NoteData());
                                                    noteArray[TrackNo][noteArray[TrackNo].Count - 1].SetBeginTick(reader.ReadElementString());
                                                }

                                                // 開始地点
                                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "playTime")
                                                {
                                                    // ノートごとのクラスの作成
                                                    noteArray[TrackNo][noteArray[TrackNo].Count - 1].SetPlayTime(reader.ReadElementString());
                                                }

                                                // デュレーション
                                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "durTick")
                                                {
                                                    noteArray[TrackNo][noteArray[TrackNo].Count - 1].SetDuration(reader.ReadElementString());
                                                }

                                                // ノートナンバー
                                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "noteNum")
                                                {
                                                    noteArray[TrackNo][noteArray[TrackNo].Count - 1].SetNoteNum(int.Parse(reader.ReadElementString()));
                                                }

                                                // 歌詞
                                                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "lyric")
                                                {
                                                    noteArray[TrackNo][noteArray[TrackNo].Count - 1].SetNoteLyric(reader.ReadElementString());
                                                    break;
                                                }
                                            }
                                        }//if note end

                                        if (reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Name == "musicalPart")
                                        {
                                            trackArray[TrackNo].SetNote(noteArray[TrackNo]);
                                            TrackNo++;
                                            break;
                                        }

                                    }//while musicalPart end

                                }//if musicalPart end

                            }//if vsTrackNo end

                        }// ノートif end
                
                    }// 一番上のwhile end

                }
                finally
                {
                    // XMLリーダーを終了
                    if (reader != null) reader.Close();
                }



                try
                {
                    writer = XmlWriter.Create(saveFileName, settings);

                    writer.WriteStartDocument();                                               // XMLドキュメント開始
                    writer.WriteStartElement("Scenario");                                      // Scenario要素開始
                    writer.WriteAttributeString("Code", deff.GetCode());   // 属性

                    writer.WriteStartElement("Sequence");                    // Sequence要素開始
                    writer.WriteAttributeString("Id", "");                   // Id属性

                    writer.WriteStartElement("Scene");                       // Sequence要素開始
                    writer.WriteAttributeString("Id", "");                   // Id属性

                    writer.WriteStartElement("Units");                       // Units要素開始


                    // ノート書き込み
                    foreach (TrackData currentTrack in trackArray)
                    {
                        writer.WriteStartElement("Unit");                                   // Unit要素開始
                        writer.WriteAttributeString("Version", "1.0");                      // Version属性
                        writer.WriteAttributeString("Id", "");                              // Id属性
                        writer.WriteAttributeString("Category", "SingerSong");              // Category属性
                        writer.WriteAttributeString("Group", currentTrack.GetTrackGuid());  // Group属性
                        writer.WriteAttributeString("StartTime", "00:00:00");               // StartTime属性
                        writer.WriteAttributeString("Duration", "10:00:00");                // Duration属性

                        writer.WriteStartElement("Song");                                   // Song要素開始
                        writer.WriteAttributeString("Version", "1.02");                     // Version属性


                        if (bpmArray != null)
                        {
                            writer.WriteStartElement("Tempo");                                      // Tempo要素開始
                            foreach (BpmData currentBpm in bpmArray)
                            {
                                writer.WriteStartElement("Sound");                                      // Sound要素開始
                                writer.WriteAttributeString("Clock", currentBpm.GetClock());            // Clock属性
                                writer.WriteAttributeString("Tempo", currentBpm.GetIntegerPoint());     // Tempo属性
                                writer.WriteEndElement();                                               // Sound終了
                            }
                            writer.WriteEndElement();                                               // Tempo終了
                        }

                        if (beatArray != null)
                        {
                            writer.WriteStartElement("Beat");                                                               // Beat要素開始
                            foreach (BeatData currentBeat in beatArray)
                            {
                                writer.WriteStartElement("Time");                                                               // Time要素開始
                                writer.WriteAttributeString("Clock", currentBeat.GetPosMes().ToString());    // Clock属性
                                writer.WriteAttributeString("Beats", currentBeat.GetNume());                  // Beats属性
                                writer.WriteAttributeString("BeatType", currentBeat.GetDenomi());            // BeatType属性
                                writer.WriteEndElement();                                                                       // Time終了
                            }
                            writer.WriteEndElement();                                                                       // Beat終了
                        }

                        writer.WriteStartElement("Score");                                                  // Score要素開始
                        writer.WriteStartElement("Key");                                                    // Key要素開始
                        writer.WriteAttributeString("Clock", "0");                                          // Clock属性
                        writer.WriteAttributeString("Fifths", "0");                                         // Fifths属性
                        writer.WriteAttributeString("Mode", "0");                                           // Mode属性
                        writer.WriteEndElement();                                                           // Key終了


                        if (currentTrack.GetNote() != null)
                        {
                            foreach (NoteData currentNote in currentTrack.GetNote())
                            {
                                writer.WriteStartElement("Note");                                           // Note要素開始
                                writer.WriteAttributeString("Clock", currentNote.GetClock());               // Clock属性
                                writer.WriteAttributeString("PitchStep", currentNote.GetPitchStep());       // PitchStep属性
                                writer.WriteAttributeString("PitchOctave", currentNote.GetPitchOctave());   // PitchOctave属性
                                writer.WriteAttributeString("Duration", currentNote.GetDuration());         // Duration属性
                                writer.WriteAttributeString("Lyric", currentNote.GetLyric());               // Lyric属性
                                writer.WriteEndElement();                                                   // Note要素終了
                            }
                        }

                        writer.WriteEndElement();               // Score終了
                        writer.WriteEndElement();               // Song終了
                        writer.WriteEndElement();               // Unit終了
                    }

                    writer.WriteEndElement();               // Units終了


                    writer.WriteStartElement("Groups");                                            // Groups要素開始

                    /*
                    writer.WriteStartElement("Group");                                             // Group要素開始
                    writer.WriteAttributeString("Version", "1.0");                                 // Version属性
                    writer.WriteAttributeString("Id", "2da68d23-4a9a-4f57-9e32-c0190f2d80ee");     // Tempo属性
                    writer.WriteAttributeString("Category", "TextVocal");                          // Category属性
                    writer.WriteAttributeString("Name", "トーク 1");                               // Name属性
                    writer.WriteAttributeString("Color", "#FF1E90FF");                             // Color属性
                    writer.WriteAttributeString("Volume", "0");                                    // Volume属性
                    writer.WriteAttributeString("Pan", "0");                                       // Pan属性
                    writer.WriteAttributeString("IsSolo", "false");                                // IsSolo属性
                    writer.WriteAttributeString("IsMuted", "false");                               // IsMuted属性
                    writer.WriteEndElement();                                                      // Group終了
                    */

                    // ノート書き込み
                    if (trackArray != null)
                    {
                        foreach (TrackData currentTrack in trackArray)
                        {
                            writer.WriteStartElement("Group");                                  // Group要素開始
                            writer.WriteAttributeString("Version", "1.0");                      // Version属性
                            writer.WriteAttributeString("Id", currentTrack.GetTrackGuid());     // Tempo属性
                            writer.WriteAttributeString("Category", "SingerSong");              // Category属性
                            writer.WriteAttributeString("Name", currentTrack.GetTrackName());   // Name属性
                            writer.WriteAttributeString("Color", "#FFFF0000");                  // Color属性
                            writer.WriteAttributeString("Volume", "0");                         // Volume属性
                            writer.WriteAttributeString("Pan", "0");                            // Pan属性
                            writer.WriteAttributeString("IsSolo", currentTrack.GetSolo().ToString());            // IsSolo属性
                            writer.WriteAttributeString("IsMuted", currentTrack.GetMute().ToString());           // IsMuted属性
                            writer.WriteEndElement();                                           // Group終了   
                        }
                    }

                    writer.WriteEndElement();                               // Groups終了

                    writer.WriteStartElement("SoundSetting");               // SoundSetting要素開始
                    writer.WriteAttributeString("Rhythm", "4/4");           // Rhythm属性
                    writer.WriteAttributeString("Tempo", "78");             // Tempo属性
                    writer.WriteEndElement();                               // SoundSetting終了

                    writer.WriteEndElement();               // Scene終了
                    writer.WriteEndElement();               // Sequence終了
                    writer.WriteEndElement();               // Scenario終了

                    writer.WriteEndDocument();              // XMLドキュメント終了
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    Console.WriteLine("" + currentFileName + "\n->\n" + saveFileName + "\n");
                    // ファイルを閉じる
                    if (writer != null) writer.Close();
                }
            }
            Console.WriteLine("Press the enter key.");
            Console.ReadLine();
        }
    }
}
