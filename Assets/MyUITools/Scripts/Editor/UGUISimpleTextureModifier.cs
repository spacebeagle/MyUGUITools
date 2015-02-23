using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System;

[InitializeOnLoad]
public class StartupUGUISimpleTextureModifier {
    static StartupUGUISimpleTextureModifier() {
        if (EditorApplication.timeSinceStartup < 10) {
            Debug.Log("Initialized TextureModifier");
            EditorUserBuildSettings.activeBuildTargetChanged += OnChangePlatform;
        }
    }

    [UnityEditor.MenuItem("Assets/Texture Util/ReImport All Compress Texture", false, 1)]
    static void OnChangePlatform() {
        Debug.Log(" TextureModifier Convert Compress Texture");
        string labels = "t:Texture";
        foreach(var type in UGUISimpleTextureModifier.compressOutputs){
            labels+=" l:"+type.ToString();
        }
      	var assets = AssetDatabase.FindAssets (labels,null);
        foreach (var asset in assets) {
            var path=AssetDatabase.GUIDToAssetPath(asset);
            if (!String.IsNullOrEmpty(path))
                AssetDatabase.ImportAsset(path);
        }
    }
}

public class UGUISimpleTextureModifier : AssetPostprocessor {
	public static readonly string KEY = "SimpleTextureModifier Enable";

	public enum TextureModifierType {
		None,
		PremultipliedAlpha,
		AlphaBleed,
		FloydSteinberg,
		Reduced16bits,
        C16bits,
        CCompressed,
        CCompressedNA,
        CCompressedWA,
        T32bits,
		T16bits,
		TCompressed,
        TCompressedNA,
        TCompressedWA,
		TPNG,
		TJPG,
	}

	static TextureFormat CompressionFormat {
		get {
			switch (EditorUserBuildSettings.activeBuildTarget) {
			case BuildTarget.Android:
				return TextureFormat.ETC_RGB4;
			case BuildTarget.iPhone:
				return TextureFormat.PVRTC_RGB4;
			default:
				return TextureFormat.DXT1;
			}
		}
	}

	static TextureFormat CompressionWithAlphaFormat {
		get {
			switch (EditorUserBuildSettings.activeBuildTarget) {
			case BuildTarget.Android:
				return TextureFormat.ETC_RGB4;
			case BuildTarget.iPhone:
				return TextureFormat.PVRTC_RGBA4;
			default:
				return TextureFormat.DXT5;
			}
		}
	}

	struct Position2 {
		public int x,y;
		public Position2(int p1, int p2)
		{
			x = p1;
			y = p2;
		}
	}
	
	readonly static List<List<Position2>> bleedTable;
	static UGUISimpleTextureModifier(){
		bleedTable=new List<List<Position2>>();
		for(int i=1;i<=12;i++){
			var bT=new List<Position2>();
			for(int x=-i;x<=i;x++){
				bT.Add(new Position2(x,i));
				bT.Add(new Position2(-x,-i));
			}
			for(int y=-i+1;y<=i-1;y++){
				bT.Add(new Position2(i,y));
				bT.Add(new Position2(-i,-y));
			}
			bleedTable.Add(bT);
		}
	}

	public readonly static List<TextureModifierType> effecters=new List<TextureModifierType>{TextureModifierType.PremultipliedAlpha,TextureModifierType.AlphaBleed};
    public readonly static List<TextureModifierType> modifiers = new List<TextureModifierType> { TextureModifierType.FloydSteinberg, TextureModifierType.Reduced16bits };
    public readonly static List<TextureModifierType> outputs = new List<TextureModifierType>{TextureModifierType.TJPG,TextureModifierType.TPNG,TextureModifierType.T32bits,TextureModifierType.T16bits,TextureModifierType.C16bits
                                                                            ,TextureModifierType.CCompressed,TextureModifierType.CCompressedNA,TextureModifierType.CCompressedWA
																			,TextureModifierType.TCompressed,TextureModifierType.TCompressedNA,TextureModifierType.TCompressedWA};
    public readonly static List<TextureModifierType> compressOutputs = new List<TextureModifierType>{
                                                                             TextureModifierType.CCompressed,TextureModifierType.CCompressedNA,TextureModifierType.CCompressedWA
                                                                             ,TextureModifierType.TCompressed,TextureModifierType.TCompressedNA,TextureModifierType.TCompressedWA};
    static void ClearLabel(List<TextureModifierType> types, bool ImportAsset = true) {
		List<UnityEngine.Object> objs=new List<UnityEngine.Object>(Selection.objects);
		foreach(var obj in objs){
			if(obj is Texture2D){
				List<string> labels=new List<string>(AssetDatabase.GetLabels(obj));
				var newLabels=new List<string>();
				labels.ForEach((string l)=>{
					if(Enum.IsDefined(typeof(TextureModifierType),l)){
						if(!types.Contains((TextureModifierType)Enum.Parse(typeof(TextureModifierType),l)))
							newLabels.Add(l);
					}
				});
				AssetDatabase.SetLabels(obj,newLabels.ToArray());
				if(ImportAsset)
					AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(obj));
			}
		}
	}

	static void SetLabel(string label,List<TextureModifierType> types){
		ClearLabel(types,false);
		List<UnityEngine.Object> objs=new List<UnityEngine.Object>(Selection.objects);
		foreach(var obj in objs){
			if(obj is Texture2D){
				List<string> labels=new List<string>(AssetDatabase.GetLabels(obj));
				labels.Add(label);
				AssetDatabase.SetLabels(obj,labels.ToArray());
				EditorUtility.SetDirty(obj);
                AssetDatabase.WriteImportSettingsIfDirty(AssetDatabase.GetAssetPath(obj));
                foreach(Editor ed in (Editor[])UnityEngine.Resources.FindObjectsOfTypeAll(typeof(Editor))){
					if(ed.target==obj){
						ed.Repaint();
						EditorUtility.SetDirty(ed);
					}
				}
			}
		}
	}

	[UnityEditor.MenuItem("Assets/Texture Util/Clear Texture Effecter Label",false,20)]
	static void ClearTextureEffecterLabel(){
		ClearLabel(effecters);
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label PremultipliedAlpha",false,20)]
	static void SetLabelPremultipliedAlpha(){
		SetLabel(TextureModifierType.PremultipliedAlpha.ToString(),effecters);
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label AlphaBleed",false,20)]
	static void SetLabelAlphaBleed(){
		SetLabel(TextureModifierType.AlphaBleed.ToString(),effecters);
	}

	[UnityEditor.MenuItem("Assets/Texture Util/Clear Texture Modifier Label",false,40)]
	static void ClearTextureModifierLabel(){
		ClearLabel(modifiers);
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label FloydSteinberg",false,40)]
	static void SetLabelFloydSteinberg(){
		SetLabel(TextureModifierType.FloydSteinberg.ToString(),modifiers);
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Reduced16bits",false,40)]
	static void SetLabelReduced16bits(){
		SetLabel(TextureModifierType.Reduced16bits.ToString(),modifiers);
	}

	[UnityEditor.MenuItem("Assets/Texture Util/Clear Texture Output Label",false,60)]
	static void ClearTextureOutputLabel(){
		ClearLabel(outputs);
	}
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Convert 16bits", false, 60)]
    static void SetLabelC16bits() {
        SetLabel(TextureModifierType.C16bits.ToString(), outputs);
    }
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Convert Compressed", false, 60)]
    static void SetLabelCCompressed() {
        SetLabel(TextureModifierType.CCompressed.ToString(), outputs);
    }
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Convert Compressed no alpha", false, 60)]
    static void SetLabelCCompressedNA() {
        SetLabel(TextureModifierType.CCompressedNA.ToString(), outputs);
    }
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Convert Compressed with alpha", false, 60)]
    static void SetLabelCCompressedWA() {
        SetLabel(TextureModifierType.CCompressedWA.ToString(), outputs);
    }
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture 16bits", false, 60)]
	static void SetLabel16bits(){
		SetLabel(TextureModifierType.T16bits.ToString(),outputs);
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture 32bits",false,60)]
	static void SetLabel32bits(){
		SetLabel(TextureModifierType.T32bits.ToString(),outputs);
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture Compressed",false,60)]
	static void SetLabelCompressed(){
		SetLabel(TextureModifierType.TCompressed.ToString(),outputs);
	}
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture Compressed no alpha", false, 60)]
    static void SetLabelCompressedNA() {
        SetLabel(TextureModifierType.TCompressedNA.ToString(), outputs);
    }
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture Compressed with alpha", false, 60)]
	static void SetLabelCompressedWA(){
		SetLabel(TextureModifierType.TCompressedWA.ToString(),outputs);
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture PNG",false,60)]
	static void SetLabelPNG(){
		SetLabel(TextureModifierType.TPNG.ToString(),outputs);
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture JPG",false,60)]
	static void SetLabelJPG(){
		SetLabel(TextureModifierType.TJPG.ToString(),outputs);
	}

	TextureModifierType effecterType=TextureModifierType.None;
	TextureModifierType modifierType=TextureModifierType.None;
	TextureModifierType outputType=TextureModifierType.None;

	void OnPreprocessTexture(){
		//return;
		var importer = (assetImporter as TextureImporter);
		UnityEngine.Object obj=AssetDatabase.LoadAssetAtPath(assetPath,typeof(Texture2D));
		var labels=new List<string>(AssetDatabase.GetLabels(obj));

		foreach(string label in labels){
			if(Enum.IsDefined(typeof(TextureModifierType),label)){
				TextureModifierType type=(TextureModifierType)Enum.Parse(typeof(TextureModifierType),label);
				if(effecters.Contains(type)){
					effecterType=type;
				}
				if(modifiers.Contains(type)){
					modifierType=type;
				}
				if(outputs.Contains(type)){
					outputType=type;
				}
			}
		}
		if(effecterType!=TextureModifierType.None || modifierType!=TextureModifierType.None || outputType!=TextureModifierType.None){
			importer.alphaIsTransparency=false;
			importer.compressionQuality = (int)TextureCompressionQuality.Best;
			if(importer.textureFormat==TextureImporterFormat.Automatic16bit)
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			else if(importer.textureFormat==TextureImporterFormat.AutomaticCompressed)
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			else if(importer.textureFormat==TextureImporterFormat.RGB16)
				importer.textureFormat = TextureImporterFormat.RGB24;
			else if(importer.textureFormat==TextureImporterFormat.RGBA16)
				importer.textureFormat = TextureImporterFormat.RGBA32;
			else if(importer.textureFormat==TextureImporterFormat.ARGB16)
				importer.textureFormat = TextureImporterFormat.ARGB32;
		}
	}
	
	void OnPostprocessTexture (Texture2D texture){
		if(effecterType==TextureModifierType.None && modifierType==TextureModifierType.None && outputType==TextureModifierType.None)
			return;
		AssetDatabase.StartAssetEditing();
		var pixels = texture.GetPixels ();
		switch (effecterType){
		case TextureModifierType.PremultipliedAlpha:{
			PremultipliedAlpha(ref pixels,texture);
			break;
		}
		case TextureModifierType.AlphaBleed:{
			AlphaBleed(ref pixels,texture);
			break;
		}}
		switch (modifierType){
		case TextureModifierType.FloydSteinberg:{
			FloydSteinberg(ref pixels,texture);
			break;
		}
		case TextureModifierType.Reduced16bits:{
			Reduced16bits(ref pixels,texture);
			break;
		}}
        //return;
        if (EditorPrefs.GetBool(KEY, false)) {
            switch (outputType) {
                case TextureModifierType.C16bits: {
                    texture.SetPixels(pixels);
                    texture.Apply(true, true);
                    EditorUtility.CompressTexture(texture, TextureFormat.RGBA4444, TextureCompressionQuality.Best); 
                    break;
                }
                case TextureModifierType.CCompressed: {
                    texture.SetPixels(pixels);
                    texture.Apply(true, true);
                    EditorUtility.CompressTexture(texture, CompressionWithAlphaFormat, TextureCompressionQuality.Best);
                    break;
                }
                case TextureModifierType.CCompressedNA: {
                    texture.SetPixels(pixels);
                    texture.Apply(true, true);
                    EditorUtility.CompressTexture(texture, CompressionFormat, TextureCompressionQuality.Best);
                    break;
                }
                case TextureModifierType.CCompressedWA: {
                    WriteAlphaTexture(pixels, texture);
                    texture.SetPixels(pixels);
                    texture.Apply(true, true);
                    EditorUtility.CompressTexture(texture, CompressionFormat, TextureCompressionQuality.Best);
                    break;
                }
                case TextureModifierType.TCompressed: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, CompressionWithAlphaFormat, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.TCompressedNA: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, CompressionFormat, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.TCompressedWA: {
                   WriteAlphaTexture(pixels, texture);
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, CompressionFormat, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.T16bits: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, TextureFormat.RGBA4444, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.T32bits: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, TextureFormat.RGBA32, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.TPNG: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true);
                   WritePNGTexture(tex, TextureFormat.RGBA32, assetPath, "RGBA.png");
                   break;
               }
               case TextureModifierType.TJPG: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true);
                   WriteJPGTexture(tex, TextureFormat.RGBA32, assetPath, "RGB.jpg");
                   break;
               }
               default: {
                   if (effecterType != TextureModifierType.None || modifierType != TextureModifierType.None) {
                       texture.SetPixels(pixels);
                       texture.Apply(true);
                   }
                   break;
                }
            }
        }
		AssetDatabase.Refresh();
		AssetDatabase.StopAssetEditing();
	}

	Texture2D BuildTexture(Texture2D texture,TextureFormat format){
		var tex = new Texture2D (texture.width, texture.height, format, texture.mipmapCount>1);
		tex.wrapMode = texture.wrapMode;
		tex.filterMode = texture.filterMode;
		tex.mipMapBias = texture.mipMapBias;
		tex.anisoLevel = texture.anisoLevel;
		return tex;
	}

	void WriteTexture(Texture2D texture,TextureFormat format,string path,string extension){
		EditorUtility.CompressTexture (texture,format,TextureCompressionQuality.Best);
		var writePath = path.Substring(0,path.LastIndexOf('.'))+extension;
		var writeAsset = AssetDatabase.LoadAssetAtPath (writePath,typeof(Texture2D)) as Texture2D;
		if (writeAsset == null) {
			AssetDatabase.CreateAsset (texture, writePath);
		} else {
			EditorUtility.CopySerialized (texture, writeAsset);
		}
	}

	void WritePNGTexture(Texture2D texture,TextureFormat format,string path,string extension){
		EditorUtility.CompressTexture (texture,format,TextureCompressionQuality.Best);
		byte[] pngData=texture.EncodeToPNG();
		//var nPath=path.Substring(0,path.LastIndexOf('.'))+extension;
		var writePath = Application.dataPath+(path.Substring(0,path.LastIndexOf('.'))+extension).Substring(6);
		File.WriteAllBytes(writePath, pngData);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	}

	void WriteJPGTexture(Texture2D texture,TextureFormat format,string path,string extension){
		EditorUtility.CompressTexture (texture,format,TextureCompressionQuality.Best);
		byte[] jpgData=texture.EncodeToJPG();
		//var nPath=path.Substring(0,path.LastIndexOf('.'))+extension;
		var writePath = Application.dataPath+(path.Substring(0,path.LastIndexOf('.'))+extension).Substring(6);
		File.WriteAllBytes(writePath, jpgData);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	}

	void WriteCompressTexture(Color[] pixels,Texture2D texture,TextureFormat format){
		var mask = BuildTexture(texture,TextureFormat.RGB24);
		for (int i = 0; i < pixels.Length; i++) {
			var a = pixels [i].a;
			pixels [i] = new Color (a, a, a);
		}
		mask.SetPixels (pixels);
		mask.Apply(true,true);
		WriteTexture(mask,CompressionFormat,assetPath,"Alpha.asset");
	}

	void WriteAlphaTexture(Color[] pixels,Texture2D texture){
		var mask = new Texture2D (texture.width, texture.height, TextureFormat.RGB24, false);
		mask.wrapMode = texture.wrapMode;
		mask.filterMode = texture.filterMode;
		mask.mipMapBias = texture.mipMapBias;
		mask.anisoLevel = texture.anisoLevel;
		var aPixels = new Color[pixels.Length];
		for (int i = 0; i < pixels.Length; i++) {
			var a = pixels [i].a;
			aPixels [i] = new Color (a, a, a);
		}
		mask.SetPixels (aPixels);
		mask.Apply(true,true);
		WriteTexture(mask,CompressionFormat,assetPath,"Alpha.asset");
	}


	void PremultipliedAlpha(ref Color[] pixels,Texture2D texture){
//		var pixels = texture.GetPixels ();
		for (int i = 0; i < pixels.Length; i++) {
			var a = pixels [i].a;
			pixels [i] = new Color (pixels[i].r*a,pixels[i].g*a,pixels[i].b*a,a);
		}
//		texture.SetPixels (pixels);
	}

	void AlphaBleed(ref Color[] pixels,Texture2D texture){
//		var pixels = texture.GetPixels ();
		var height = texture.height;
		var width = texture.width;
		// Debug.Log(texture.format);
		for (var y = 0; y < height; y++) {
			for (var x = 0; x < width; x++) {
				int position=y*width+x;
				if (pixels [position].a <= 0.125f) {
					float a=pixels[position].a;
					pixels[position]=new Color(0.5f,0.5f,0.5f,a);
					int index=1;
					foreach(var bt in bleedTable){
						float r=0.0f;
						float g=0.0f;
						float b=0.0f;
						float c=0.0f;
						foreach(var pt in bt){
							int xp=x+pt.x;
							int yp=y+pt.y;
							if (xp >= 0 && xp < width && yp >= 0 && yp < height)
							{
								int pos=yp*width+xp;
								float ad=pixels[pos].a;
								if(ad>0.125f){
									r+=pixels[pos].r*ad;	
									g+=pixels[pos].g*ad;	
									b+=pixels[pos].b*ad;
									c+=ad;
								}
							}
						}
						if(c>0.0f){
							float fac=Mathf.Min (1.0f,(float)(13-index)/6.0f);
							pixels[position]=
								new Color(r/c*fac+pixels[position].r*(1.0f-fac)
								          ,g/c*fac+pixels[position].g*(1.0f-fac)
								          ,b/c*fac+pixels[position].b*(1.0f-fac),a);
							break;
						}
						index++;
					}
				}
			}
		}
//		texture.SetPixels (pixels);
	}

	const float k1Per256 = 1.0f / 255.0f;
	const float k1Per16 = 1.0f / 15.0f;
	const float k3Per16 = 3.0f / 15.0f;
	const float k5Per16 = 5.0f / 15.0f;
	const float k7Per16 = 7.0f / 15.0f;

	void Reduced16bits(ref Color[] pixels,Texture2D texture){
		var texw = texture.width;
		var texh = texture.height;
		
//		var pixels = texture.GetPixels ();
		var offs = 0;
		for (var y = 0; y < texh; y++) {
			for (var x = 0; x < texw; x++) {
				float a = pixels [offs].a;
				float r = pixels [offs].r;
				float g = pixels [offs].g;
				float b = pixels [offs].b;
				
				var a2 = Mathf.Round(a * 15.0f) * k1Per16;
				var r2 = Mathf.Round(r * 15.0f) * k1Per16;
				var g2 = Mathf.Round(g * 15.0f) * k1Per16;
				var b2 = Mathf.Round(b * 15.0f) * k1Per16;

				pixels [offs].a = a2;
				pixels [offs].r = r2;
				pixels [offs].g = g2;
				pixels [offs].b = b2;
				offs++;
			}
		}
//		texture.SetPixels (pixels);
	}

	void FloydSteinberg(ref Color[] pixels,Texture2D texture){
		var texw = texture.width;
		var texh = texture.height;
		
//		var pixels = texture.GetPixels ();
		var offs = 0;
		
		for (var y = 0; y < texh; y++) {
			for (var x = 0; x < texw; x++) {
				float a = pixels [offs].a;
				float r = pixels [offs].r;
				float g = pixels [offs].g;
				float b = pixels [offs].b;
				
				var a2 = Mathf.Round(a * 15.0f) * k1Per16;
				var r2 = Mathf.Round(r * 15.0f) * k1Per16;
				var g2 = Mathf.Round(g * 15.0f) * k1Per16;
				var b2 = Mathf.Round(b * 15.0f) * k1Per16;
				
				var ae = Mathf.Round((a - a2)*255.0f)*k1Per256;
				var re = Mathf.Round((r - r2)*255.0f)*k1Per256;
				var ge = Mathf.Round((g - g2)*255.0f)*k1Per256;
				var be = Mathf.Round((b - b2)*255.0f)*k1Per256;
				
				pixels [offs].a = a2;
				pixels [offs].r = r2;
				pixels [offs].g = g2;
				pixels [offs].b = b2;
				
				var n1 = offs + 1;
				var n2 = offs + texw - 1;
				var n3 = offs + texw;
				var n4 = offs + texw + 1;
				
				if (x < texw - 1) {
					pixels [n1].a += ae * k7Per16;
					pixels [n1].r += re * k7Per16;
					pixels [n1].g += ge * k7Per16;
					pixels [n1].b += be * k7Per16;
				}
				
				if (y < texh - 1) {
					pixels [n3].a += ae * k5Per16;
					pixels [n3].r += re * k5Per16;
					pixels [n3].g += ge * k5Per16;
					pixels [n3].b += be * k5Per16;
					
					if (x > 0) {
						pixels [n2].a += ae * k3Per16;
						pixels [n2].r += re * k3Per16;
						pixels [n2].g += ge * k3Per16;
						pixels [n2].b += be * k3Per16;
					}
					
					if (x < texw - 1) {
						pixels [n4].a += ae * k1Per16;
						pixels [n4].r += re * k1Per16;
						pixels [n4].g += ge * k1Per16;
						pixels [n4].b += be * k1Per16;
					}
				}
				offs++;
			}
		}
//		texture.SetPixels (pixels);
	}
}
