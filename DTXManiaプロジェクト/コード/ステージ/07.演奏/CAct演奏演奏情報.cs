﻿using System;
using System.Collections.Generic;
using System.Text;
using FDK;

namespace DTXMania
{
	internal class CAct演奏演奏情報 : CActivity
	{
		// プロパティ

		public double dbBPM;
		public int n小節番号;


		// コンストラクタ

		public CAct演奏演奏情報()
		{
			base.b活性化してない = true;
		}

				
		// CActivity 実装

		public override void On活性化()
		{
			this.n小節番号 = 0;
			this.dbBPM = CDTXMania.DTX.BASEBPM + CDTXMania.DTX.BPM;
			base.On活性化();
		}
		public override int On進行描画()
		{
			throw new InvalidOperationException( "t進行描画(int x, int y) のほうを使用してください。" );
		}
		public void t進行描画( int x, int y )
		{
			if( !base.b活性化してない )
			{
				y += 0x153;
				CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "BGMAdjust: {0:####0} ms", CDTXMania.DTX.nBGMAdjust ) );
				y -= 0x10;
				int num = ( CDTXMania.DTX.listChip.Count > 0 ) ? CDTXMania.DTX.listChip[ CDTXMania.DTX.listChip.Count - 1 ].n発声時刻ms : 0;
				string str = "Time:      " + ( ( ( (double) CDTXMania.Timer.n現在時刻 ) / 1000.0 ) ).ToString( "####0.00" ) + " / " + ( ( ( (double) num ) / 1000.0 ) ).ToString( "####0.00" );
				CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, str );
				y -= 0x10;
				CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Part:      {0:####0}", this.n小節番号 ) );
				y -= 0x10;
				CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "BPM:       {0:####0.00}", this.dbBPM ) );
				y -= 0x10;
				CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Frame:     {0:####0} fps", CDTXMania.FPS.n現在のFPS ) );
				y -= 0x10;
			}
		}
	}
}
