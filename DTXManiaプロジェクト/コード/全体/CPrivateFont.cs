﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using SlimDX;
using FDK;

namespace DTXMania
{
	/// <summary>
	/// プライベートフォントでの描画を扱うクラス。
	/// </summary>
	/// <exception cref="ArgumentException">スタイル指定不正時に例外発生</exception>
	/// <remarks>
	/// 簡単な使い方
	/// CPrivateFont prvFont = new CPrivateFont( CSkin.Path( @"Graphics\fonts\mplus-1p-bold.ttf" ), 36 );	// プライベートフォント
	/// とか
	/// CPrivateFont prvFont = new CPrivateFont( new FontFamily("MS UI Gothic"), 36, FontStyle.Bold );		// システムフォント
	/// とかした上で、
	/// CTexture ctBMP = prvFont.DrawPrivateFont( "ABCDE", Color.White, Color.Black );						// フォント色＝白、縁の色＝黒の例。縁の色は省略可能
	/// とか
	/// CTexture ctBMP = prvFont.DrawPrivateFont( "ABCDE", Color.White, Color.Black, Color.Yellow, Color.OrangeRed ); // 上下グラデーション(Yellow→OrangeRed)
	/// とかして、
	/// ctBMP.t2D描画( ～～～ );
	/// で表示してください。
	/// (任意のフォントでのレンダリングは結構負荷が大きいので、描画フレーム毎にフォントを再レンダリングするようなことはせず、
	///  一旦テクスチャにレンダリングして、それを描画に使い回すようにしてください。)
	/// </remarks
	public class CPrivateFont : IDisposable
	{
		// コンストラクタ
		public CPrivateFont( FontFamily fontfamily, int pt, FontStyle style )
		{
			Initialize( null, fontfamily, pt, style );
		}
		public CPrivateFont( FontFamily fontfamily, int pt )
		{
			Initialize( null, fontfamily, pt, FontStyle.Regular );
		}
		public CPrivateFont( string fontpath, int pt, FontStyle style )
		{
			Initialize( fontpath, null, pt, style );
		}
		public CPrivateFont( string fontpath, int pt )
		{
			Initialize( fontpath, null, pt, FontStyle.Regular );
		}

		private void Initialize( string fontpath, FontFamily fontfamily, int pt, FontStyle style )
		{
			this._pfc = null;
			this._fontfamily = null;
			this._font = null;
			this._pt = pt;

			if ( fontfamily != null )
			{
				this._fontfamily = fontfamily;
			}
			else
			{
				try
				{
					this._pfc = new System.Drawing.Text.PrivateFontCollection();	//PrivateFontCollectionオブジェクトを作成する
					this._pfc.AddFontFile( fontpath );								//PrivateFontCollectionにフォントを追加する
				}
				catch ( System.IO.FileNotFoundException )
				{
					Trace.TraceError( "プライベートフォントの追加に失敗しました。({0})", fontpath );
					return;
				}

				//foreach ( FontFamily ff in pfc.Families )
				//{
				//    Debug.WriteLine( "fontname=" + ff.Name );
				//    if ( ff.Name == Path.GetFileNameWithoutExtension( fontpath ) )
				//    {
				//        _fontfamily = ff;
				//        break;
				//    }
				//}
				//if ( _fontfamily == null )
				//{
				//    Trace.TraceError( "プライベートフォントの追加後、検索に失敗しました。({0})", fontpath );
				//    return;
				//}
				_fontfamily = _pfc.Families[ 0 ];
			}

			// 指定されたフォントスタイルが適用できない場合は、フォント内で定義されているスタイルから候補を選んで使用する
			// 何もスタイルが使えないようなフォントなら、例外を出す。
			if ( !_fontfamily.IsStyleAvailable( style ) )
			{
				FontStyle[] FS = { FontStyle.Regular, FontStyle.Bold, FontStyle.Italic, FontStyle.Underline, FontStyle.Strikeout };
				style = FontStyle.Regular | FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout;	// null非許容型なので、代わりに全盛をNGワードに設定
				foreach ( FontStyle ff in FS )
				{
					if ( this._fontfamily.IsStyleAvailable( ff ) )
					{
						style = ff;
						Trace.TraceWarning( "フォント{0}へのスタイル指定を、{1}に変更しました。", Path.GetFileName( fontpath ), style.ToString() );
						break;
					}
				}
				if ( style == ( FontStyle.Regular | FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout ) )
				{
					throw new ArgumentException( "フォント{0}は適切なスタイルを選択できず、使用できません。", Path.GetFileName( fontpath ) );
				}
			}
			this._font = new Font( this._fontfamily, pt, style );			//PrivateFontCollectionの先頭のフォントのFontオブジェクトを作成する
		}

		[Flags]
		private enum DrawMode
		{
			Normal,
			Edge,
			Gradation
		}

		#region [ DrawPrivateFontのオーバーロード群 ]
#if こちらは使わない // (CTextureではなく、Bitmapを返す版)
		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <returns>描画済テクスチャ</returns>
		public Bitmap DrawPrivateFont( string drawstr, Color fontColor )
		{
			return DrawPrivateFont( drawstr, DrawMode.Normal, fontColor, Color.White, Color.White, Color.White );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <returns>描画済テクスチャ</returns>
		public Bitmap DrawPrivateFont( string drawstr, Color fontColor, Color edgeColor )
		{
			return DrawPrivateFont( drawstr, DrawMode.Edge, fontColor, edgeColor, Color.White, Color.White );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		//public Bitmap DrawPrivateFont( string drawstr, Color fontColor, Color gradationTopColor, Color gradataionBottomColor )
		//{
		//    return DrawPrivateFont( drawstr, DrawMode.Gradation, fontColor, Color.White, gradationTopColor, gradataionBottomColor );
		//}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		public Bitmap DrawPrivateFont( string drawstr, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradataionBottomColor )
		{
			return DrawPrivateFont( drawstr, DrawMode.Edge | DrawMode.Gradation, fontColor, edgeColor, gradationTopColor, gradataionBottomColor );
		}
#endif
		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <returns>描画済テクスチャ</returns>
		public CTexture DrawPrivateFont( string drawstr, Color fontColor )
		{
			Bitmap bmp = DrawPrivateFont( drawstr, DrawMode.Normal, fontColor, Color.White, Color.White, Color.White );
			return CDTXMania.tテクスチャの生成( bmp, false );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <returns>描画済テクスチャ</returns>
		public CTexture DrawPrivateFont( string drawstr, Color fontColor, Color edgeColor )
		{
			Bitmap bmp = DrawPrivateFont( drawstr, DrawMode.Edge, fontColor, edgeColor, Color.White, Color.White );
			return CDTXMania.tテクスチャの生成( bmp, false );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		//public Bitmap DrawPrivateFont( string drawstr, Color fontColor, Color gradationTopColor, Color gradataionBottomColor )
		//{
		//    Bitmap bmp = DrawPrivateFont( drawstr, DrawMode.Gradation, fontColor, Color.White, gradationTopColor, gradataionBottomColor );
		//	  return CDTXMania.tテクスチャの生成( bmp, false );
		//}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		public CTexture DrawPrivateFont( string drawstr, Color fontColor, Color edgeColor,  Color gradationTopColor, Color gradataionBottomColor )
		{
			Bitmap bmp = DrawPrivateFont( drawstr, DrawMode.Edge | DrawMode.Gradation, fontColor, edgeColor, gradationTopColor, gradataionBottomColor );
			return CDTXMania.tテクスチャの生成( bmp, false );
		}
		#endregion


		/// <summary>
		/// 文字列を描画したテクスチャを返す(メイン処理)
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="drawmode">描画モード</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		private Bitmap DrawPrivateFont( string drawstr, DrawMode drawmode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor )
		{
			if ( this._fontfamily == null )
			{
				// nullを返すと、その後bmp→texture処理や、textureのサイズを見て・・の処理で全部例外が発生することになる。
				// それは非常に面倒なので、最小限のbitmapを返してしまう。
				// まずはこの仕様で進めますが、問題有れば(上位側からエラー検出が必要であれば)例外を出したりエラー状態であるプロパティを定義するなり検討します。
				return new Bitmap(1, 1);
			}
			bool bEdge =      ( ( drawmode & DrawMode.Edge      ) == DrawMode.Edge );
			bool bGradation = ( ( drawmode & DrawMode.Gradation ) == DrawMode.Gradation );

			// 縁取りの縁のサイズは、とりあえずフォントの大きさの1/4とする
			int nEdgePt = (bEdge)? _pt / 4 : 0;

			// 描画サイズを測定する
			Size stringSize = System.Windows.Forms.TextRenderer.MeasureText( drawstr, this._font );

			//取得した描画サイズを基に、描画先のbitmapを作成する
			Bitmap bmp = new Bitmap( stringSize.Width + nEdgePt * 2, stringSize.Height + nEdgePt * 2 );
			bmp.MakeTransparent();
			Graphics g = Graphics.FromImage( bmp );
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

			StringFormat sf = new StringFormat();
			sf.LineAlignment = StringAlignment.Far;	// 画面下部（垂直方向位置）
			sf.Alignment = StringAlignment.Center;	// 画面中央（水平方向位置）

			// レイアウト枠
			Rectangle r = new Rectangle( 0, 0, stringSize.Width + nEdgePt * 2, stringSize.Height + nEdgePt * 2 );

			if ( bEdge )	// 縁取り有りの描画
			{
				// DrawPathで、ポイントサイズを使って描画するために、DPIを使って単位変換する
				// (これをしないと、単位が違うために、小さめに描画されてしまう)
				float sizeInPixels = _font.SizeInPoints * g.DpiY / 72;  // 1 inch = 72 points

				System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
				gp.AddString( drawstr, this._fontfamily, (int) this._font.Style, sizeInPixels, r, sf );

				// 縁取りを描画する
				Pen p = new Pen( edgeColor, nEdgePt );
				p.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
				g.DrawPath( p, gp );

				// 塗りつぶす
				Brush br;
				if ( bGradation )
				{
					br = new LinearGradientBrush( r, gradationTopColor, gradationBottomColor, LinearGradientMode.Vertical );
				}
				else
				{
					br = new SolidBrush( fontColor );
				}
				g.FillPath( br, gp );

				if ( br != null ) br.Dispose(); br = null;
				if ( p != null ) p.Dispose(); p = null;
				if ( gp != null ) gp.Dispose(); gp = null;
			}
			else
			{
				// 縁取りなしの描画
				System.Windows.Forms.TextRenderer.DrawText( g, drawstr, _font, new Point( 0, 0 ), fontColor );
			}
#if debug表示
			g.DrawRectangle( new Pen( Color.White, 1 ), new Rectangle( 1, 1, stringSize.Width-1, stringSize.Height-1 ) );
			g.DrawRectangle( new Pen( Color.Green, 1 ), new Rectangle( 0, 0, bmp.Width - 1, bmp.Height - 1 ) );
#endif

			#region [ リソースを解放する ]
			if ( sf != null )	sf.Dispose();	sf = null;
			if ( g != null )	g.Dispose();	g = null;
			#endregion

			return bmp;
		}


		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if ( !this.bDispose完了済み )
			{
				if ( this._font != null )
				{
					this._font.Dispose();
					this._font = null;
				}
				if ( this._pfc != null )
				{
					this._pfc.Dispose();
					this._pfc = null;
				}

				this.bDispose完了済み = true;
			}
		}
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		private bool bDispose完了済み;
		private System.Drawing.Text.PrivateFontCollection _pfc;
		private Font _font;
		private FontFamily _fontfamily;
		private int _pt;
		//-----------------
		#endregion
	}
}
