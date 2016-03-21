﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DTXCreator.MIDIインポート
{
    class CMIDIトラック
    {
        public string strトラック名;
        public int nトラック数;
        public int nデータ長;
        public int nチャンネル;
        public byte[] byMIDIトラックバイナリ;
        public string str解析内容;
        public CMIDI cMIDI;

        public CMIDIトラック( CMIDI _cMIDI, int _dトラック数, byte[] _byMIDIトラックバイナリ )
        {
            this.strトラック名 = "";
            this.nトラック数 = _dトラック数;
            this.nデータ長 = 0;
            this.nチャンネル = -1;
            this.byMIDIトラックバイナリ = _byMIDIトラックバイナリ;
            this.cMIDI = _cMIDI;
			this.str解析内容 = "";

            this.nデータ長 = CMIDI.nBin2Int( this.byMIDIトラックバイナリ, 4, 4 );
        }

        public void tトラックチャンクを走査する()
        {
            int p = 8;
            UInt32 nデルタタイム合計 = 0;
			int nイベントbefore = 0;

            while( true )
            {
				// デルタタイム計算
                int nデルタタイムLen = 0;
				UInt32 deltatime = 0;
				for ( int i = 0; i < 3; i++ )		// デルタタイムは最大4byte
				{
					++nデルタタイムLen;
					UInt32 b = this.byMIDIトラックバイナリ[ p + i ];
					deltatime <<= 7;
					deltatime += ( b & 0x7F );		// 下位7bitのみ使用
					if ( b < 0x80 ) break;			// MSBが0になったらデルタタイム終了
				}
				nデルタタイム合計 += deltatime;

				// イベント
                int nイベント = this.byMIDIトラックバイナリ[p+nデルタタイムLen];
                int nイベントLen = 3;

				// デルタタイムの直後がイベントじゃなかったら、前のイベントを継ぐ
				if ( nイベント < 128 && nイベントbefore >= 128 && nイベントbefore < 160 )
				{
					nイベント = nイベントbefore;
					p -= 1;
				}
                
                // 8n - 9n ノートオフ・オン
                if ( nイベント >= 128 && nイベント < 160 )
                {
                    int nData1 = this.byMIDIトラックバイナリ[p+nデルタタイムLen+1];
                    int nData2 = this.byMIDIトラックバイナリ[p+nデルタタイムLen+2];
                    // ノートオン(9n)の時の値を取得
                    if ( nイベント >= 144 )
                    {
                        this.nチャンネル = nイベント - 144 + 1;
                        if ( this.nチャンネル == cMIDI.n読み込みCh )
                        {
                            cMIDI.lチップ.Add( new CMIDIチップ( cMIDI, (int)nデルタタイム合計, nData1, nData2 ) );
                            cMIDI.nドラム各ノート数[nData1] ++;
                            //this.str解析内容 += "Drum  / Tick: "+nデルタタイム合計.ToString().PadLeft(6)+" Note: "+nData1.ToString("X2")+"\r\n";
                        }
                    }
					//this.str解析内容 += ((nイベント>=144)?"N-ON ":"N-OFF")+" "+p.ToString().PadLeft(6)+" "+nデルタタイム[0]+","+nData1.ToString("X2")+","+nData2.ToString("X2")+"\r\n";
                    
                    nイベントLen = 3;
                }
                // A0 - EF コントロールチェンジ等
                else if ( nイベント >= 160 && nイベント < 240 )
                {
                    int nData1 = this.byMIDIトラックバイナリ[p+nデルタタイムLen+1];
                    int nData2 = this.byMIDIトラックバイナリ[p+nデルタタイムLen+2];
                    
                    nイベントLen = 3;
                    if ( nイベント >= 192 && nイベント < 224 ) nイベントLen = 2;

                    //this.str解析内容 += "CC    / Tick: "+nデルタタイム合計.ToString().PadLeft(6)+" Type: "+nData1.ToString("X2")+"\r\n";
                }
                // F0 システム？
                else if ( nイベント.ToString("X2") == "F0" )
                {
                    nイベントLen = 1;
                    string str = "";
                    for ( int si = 1; si < 128; si++ )
                    {
                        if (this.byMIDIトラックバイナリ[p + nデルタタイムLen + si].ToString("X2") == "F7")
                        {
                            nイベントLen = 1 + si;
                            str = CMIDI.strBin2BinStr( this.byMIDIトラックバイナリ, p + nデルタタイムLen, nイベントLen );
                            break;
                        }
                    }

                    //this.str解析内容 += "Sys   / Tick: "+nデルタタイム合計.ToString().PadLeft(6)+" Val : "+str+"\r\n";
                }
                // FF メタイベント
                else if ( nイベント.ToString("X2") == "FF" )
                {
                    int nType = this.byMIDIトラックバイナリ[p+nデルタタイムLen+1];
                    int nLen = 0;

                    switch( nType.ToString("X2") )
                    {
                        // FF 01 - FF 07
                        case "01" :
                        case "02" :
                        case "03" :
                        case "04" :
                        case "05" :
                        case "06" :
                        case "07" :
                            nLen = this.byMIDIトラックバイナリ[p+nデルタタイムLen+2];
                            string str1 = CMIDI.strBin2Str( this.byMIDIトラックバイナリ, p+nデルタタイムLen+3, nLen );
                            if ( nType.ToString("X2") == "03" ) this.strトラック名 = str1;
                            nイベントLen = 3 + nLen;
                            break;
                        
                        // FF 20 - FF 21
                        case "20" :
                        case "21" :
                            nイベントLen = 4;
                            break;

                        // FF 2F EOT
                        case "2F" :
                            nイベントLen = 0;
                            break;

                        // FF 51 BPM
                        case "51" :
                            cMIDI.dBPM = Math.Round( (double) 60.0 * Math.Pow(10,6) / CMIDI.nBin2Int( this.byMIDIトラックバイナリ, p+nデルタタイムLen+3, 3 ), 2 );
                            nイベントLen = 6;
                            break;

                        // FF 54
                        case "54" :
                            nイベントLen = 8;
                            break;

                        // FF 58
                        case "58" :
                            // 拍設定 格納だけして何もしてない
                            cMIDI.strTimeSignature = CMIDI.strBin2BinStr( this.byMIDIトラックバイナリ, p+nデルタタイムLen+3, 4 );
                            nイベントLen = 7;
                            break;

                        // FF 59
                        case "59" :
                            nイベントLen = 5;
                            break;

                    }
                    
                    //this.str解析内容 += "Event / Tick: "+nデルタタイム合計.ToString().PadLeft(6)+" Type: "+nType.ToString("X2")+"\r\n";
                }
				nイベントbefore = nイベント;
                
                p += nデルタタイムLen + nイベントLen;
                
                if ( nイベントLen == 0 ) // EOT
                {
                    //this.str解析内容 += "End Of Track\r\n";
                    break;
                }
                if ( p >= this.nデータ長 + 8 )
                {
                    //this.str解析内容 += "Pointer Over "+p+"\r\n";
                    break;
                }
            }
        }

    }
    
}
