using System;
using System.Reflection;
using Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Tests.Editor {
	public class AsepriteImporterSpec {
		private AsepriteImporter _asepriteImporter = new AsepriteImporter();
		private MethodInfo _getSpritesFromAssetPathMethod;
		private MethodInfo _countFramesInTagMethod;

		private string _baseTexturePath = "Packages/net.niwaiwai.aseprite_importer/Tests/Editor/AsepriteImporterSpecBase.png";
		private string _texturePath = "Packages/net.niwaiwai.aseprite_importer/Tests/Editor/AsepriteImporterSpec.png";
		private string _jsonPath = "Packages/net.niwaiwai.aseprite_importer/Tests/Editor/AsepriteImporterSpec.json";
		private string _animatorControllerPath = "Packages/net.niwaiwai.aseprite_importer/Tests/Editor/AsepriteImporterSpec.controller";
		private string _animationClipPath1 = "Packages/net.niwaiwai.aseprite_importer/Tests/Editor/AsepriteImporterSpec__Anim1.anim";
		private string _animationClipPath2 = "Packages/net.niwaiwai.aseprite_importer/Tests/Editor/AsepriteImporterSpec__Anim2.anim";
		private string _animationClipPath3 = "Packages/net.niwaiwai.aseprite_importer/Tests/Editor/AsepriteImporterSpec__Anim3.anim";

		[OneTimeSetUp]
		public void OneTimeSetUp() {
			Type type = _asepriteImporter.GetType();
			_getSpritesFromAssetPathMethod = type.GetMethod("GetSpritesFromAssetPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new [] { typeof(string), typeof(AsepriteSpriteFrameTagInfo[]) }, null);
			_countFramesInTagMethod = type.GetMethod("CountFramesInTag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new [] { typeof(AsepriteSpriteFrameTagInfo) }, null);
		}

		[SetUp]
		public void SetUp() {
			AssetDatabase.CopyAsset(_baseTexturePath, _texturePath);
		}

		[TearDown]
		public void TearDown() {
			AssetDatabase.DeleteAsset(_texturePath);
			AssetDatabase.DeleteAsset(_animatorControllerPath);
			AssetDatabase.DeleteAsset(_animationClipPath1);
			AssetDatabase.DeleteAsset(_animationClipPath2);
			AssetDatabase.DeleteAsset(_animationClipPath3);
		}

		[Test]
		public void ShouldSlicedTextureSpritesAmountSameAsAmountThatDefinedInJson() {
			var data = EditorTestUtil.GetData(_jsonPath);
			_asepriteImporter.Import(EditorTestUtil.GetTexture(_texturePath), data, new Vector2Int(0, 0));

			var sprites = (object[])_getSpritesFromAssetPathMethod.Invoke(_asepriteImporter, new object[] { _texturePath, data.meta.frameTags });
			Assert.AreEqual(12, sprites.Length);
		}

		[Test]
		public void ShouldSlicedTextureSpritesOrderSameAsOrderThatDefinedInJson() {
			var data = EditorTestUtil.GetData(_jsonPath);
			_asepriteImporter.Import(EditorTestUtil.GetTexture(_texturePath), data, new Vector2Int(0, 0));
			
			var sprites = (Sprite[])_getSpritesFromAssetPathMethod.Invoke(_asepriteImporter, new object[] { _texturePath, data.meta.frameTags });

			var frameCount = 0;
			for (int i = 0; i < data.meta.frameTags.Length; i++) {
				var tag = data.meta.frameTags[i];
				var frameAmount = (int) _countFramesInTagMethod.Invoke(_asepriteImporter, new object[] { tag });
				for (int j = 0; j < frameAmount; j++) {
					Assert.AreEqual(tag.name + "__" + (j + 1), sprites[frameCount].name);
					frameCount++;
				}
			}
		}

		[Test]
		public void ShouldSlicedTextureSpritesDimensionSameAsDimensionThatDefinedInJson() {
			var data = EditorTestUtil.GetData(_jsonPath);
			_asepriteImporter.Import(EditorTestUtil.GetTexture(_texturePath), data, new Vector2Int(0, 0));
			
			var sprites = (Sprite[])_getSpritesFromAssetPathMethod.Invoke(_asepriteImporter, new object[] { _texturePath, data.meta.frameTags });

			var frameCount = 0;
			for (int i = 0; i < data.meta.frameTags.Length; i++) {
				var tag = data.meta.frameTags[i];
				var frameAmount = (int) _countFramesInTagMethod.Invoke(_asepriteImporter, new object[] { tag });
				for (int j = 0; j < frameAmount; j++) {
					Assert.AreEqual(data.frames[frameCount].frame.w, sprites[frameCount].rect.width);
					Assert.AreEqual(data.frames[frameCount].frame.h, sprites[frameCount].rect.height);
					frameCount++;
				}
			}
		}

		[Test]
		public void ShouldAnimationClipGeneratedSameNameAndDirectoryAsTextureByEachTagsDefinedInJson() {
			var data = EditorTestUtil.GetData(_jsonPath);
			_asepriteImporter.Import(EditorTestUtil.GetTexture(_texturePath), data, new Vector2Int(0, 0));

			Assert.NotNull(AssetDatabase.LoadAssetAtPath<AnimationClip>(_animationClipPath1));
			Assert.NotNull(AssetDatabase.LoadAssetAtPath<AnimationClip>(_animationClipPath2));
			Assert.NotNull(AssetDatabase.LoadAssetAtPath<AnimationClip>(_animationClipPath3));
		}

		[Test]
		public void ShouldGuidWillNotUpdatedWhenOverwriteAnimationClip() {
			var data = EditorTestUtil.GetData(_jsonPath);
			_asepriteImporter.Import(EditorTestUtil.GetTexture(_texturePath), data, new Vector2Int(0, 0));

			var guid1 = AssetDatabase.AssetPathToGUID(_animationClipPath1);
			var guid2 = AssetDatabase.AssetPathToGUID(_animationClipPath2);
			var guid3 = AssetDatabase.AssetPathToGUID(_animationClipPath3);
			
			_asepriteImporter.Import(EditorTestUtil.GetTexture(_texturePath), data, new Vector2Int(0, 0));

			Assert.AreEqual(guid1, AssetDatabase.AssetPathToGUID(_animationClipPath1));
			Assert.AreEqual(guid2, AssetDatabase.AssetPathToGUID(_animationClipPath2));
			Assert.AreEqual(guid3, AssetDatabase.AssetPathToGUID(_animationClipPath3));
		}

		[Test]
		public void ShouldAnimatorControllerGeneratedSameNameAndDirectoryAsTexture() {
			var data = EditorTestUtil.GetData(_jsonPath);
			_asepriteImporter.Import(EditorTestUtil.GetTexture(_texturePath), data, new Vector2Int(0, 0));

			Assert.NotNull(AssetDatabase.LoadAssetAtPath<AnimatorController>(_animatorControllerPath));
		}

		[Test]
		public void ShouldGuidWillNotUpdatedWhenOverwriteAnimatorController() {
			var data = EditorTestUtil.GetData(_jsonPath);
			_asepriteImporter.Import(EditorTestUtil.GetTexture(_texturePath), data, new Vector2Int(0, 0));
			var guid = AssetDatabase.AssetPathToGUID(_animatorControllerPath);
			
			_asepriteImporter.Import(EditorTestUtil.GetTexture(_texturePath), data, new Vector2Int(0, 0));
			Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(_animatorControllerPath));
		}
	}
}

