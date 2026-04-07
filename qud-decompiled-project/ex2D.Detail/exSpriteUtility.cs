using UnityEngine;

namespace ex2D.Detail;

public static class exSpriteUtility
{
	public static exSprite NewSimpleSprite(GameObject _go, exTextureInfo _info, int _width, int _height, Color _color)
	{
		exSprite exSprite = _go.GetComponent<exSprite>();
		if (exSprite == null)
		{
			exSprite = _go.AddComponent<exSprite>();
		}
		if (exSprite.shader == null)
		{
			exSprite.shader = Shader.Find("ex2D/Alpha Blended");
		}
		exSprite.spriteType = exSpriteType.Simple;
		exSprite.textureInfo = _info;
		exSprite.customSize = true;
		exSprite.width = _width;
		exSprite.height = _height;
		exSprite.color = _color;
		return exSprite;
	}

	public static exSprite NewSlicedSprite(GameObject _go, exTextureInfo _info, int _left, int _right, int _top, int _bottom, int _width, int _height, Color _color, bool _borderOnly)
	{
		exSprite exSprite = _go.GetComponent<exSprite>();
		if (exSprite == null)
		{
			exSprite = _go.AddComponent<exSprite>();
		}
		if (exSprite.shader == null)
		{
			exSprite.shader = Shader.Find("ex2D/Alpha Blended");
		}
		exSprite.spriteType = exSpriteType.Sliced;
		exSprite.textureInfo = _info;
		exSprite.borderOnly = _borderOnly;
		exSprite.customBorderSize = true;
		exSprite.leftBorderSize = _left;
		exSprite.rightBorderSize = _right;
		exSprite.topBorderSize = _top;
		exSprite.bottomBorderSize = _bottom;
		exSprite.customSize = true;
		exSprite.width = _width;
		exSprite.height = _height;
		exSprite.color = _color;
		return exSprite;
	}

	public static void GetDicingCount(exTextureInfo _ti, out int _colCount, out int _rowCount)
	{
		_colCount = 1;
		_rowCount = 1;
		if (_ti != null)
		{
			if (_ti.diceUnitWidth > 0 && _ti.width > 0)
			{
				_colCount = Mathf.CeilToInt((float)_ti.width / (float)_ti.diceUnitWidth);
			}
			if (_ti.diceUnitHeight > 0 && _ti.height > 0)
			{
				_rowCount = Mathf.CeilToInt((float)_ti.height / (float)_ti.diceUnitHeight);
			}
		}
	}

	public static void GetTilingCount(exISprite _sprite, out int _colCount, out int _rowCount)
	{
		exTextureInfo textureInfo = _sprite.textureInfo;
		if (textureInfo != null && (float)textureInfo.width + _sprite.tiledSpacing.x != 0f && (float)textureInfo.height + _sprite.tiledSpacing.y != 0f)
		{
			_colCount = Mathf.Max(Mathf.CeilToInt(_sprite.width / ((float)textureInfo.width + _sprite.tiledSpacing.x)), 1);
			_rowCount = Mathf.Max(Mathf.CeilToInt(_sprite.height / ((float)textureInfo.height + _sprite.tiledSpacing.y)), 1);
		}
		else
		{
			_colCount = 1;
			_rowCount = 1;
		}
	}

	public static void SetTextureInfo(exSpriteBase _sprite, ref exTextureInfo _ti, exTextureInfo _newTi, bool _useTextureOffset, exSpriteType _spriteType)
	{
		exTextureInfo exTextureInfo = _ti;
		_ti = _newTi;
		if (!(_newTi != null))
		{
			return;
		}
		if (_newTi.texture == null)
		{
			Debug.LogWarning("invalid textureInfo");
		}
		switch (_spriteType)
		{
		case exSpriteType.Tiled:
			if (exTextureInfo == null || (object)exTextureInfo == _newTi || _newTi.width != exTextureInfo.width || _newTi.height != exTextureInfo.height)
			{
				(_sprite as exISprite).UpdateBufferSize();
				_sprite.updateFlags |= exUpdateFlags.Vertex;
			}
			break;
		case exSpriteType.Diced:
			(_sprite as exISprite).UpdateBufferSize();
			_sprite.updateFlags |= exUpdateFlags.Vertex;
			break;
		default:
			if (!_sprite.customSize && (exTextureInfo == null || _newTi.width != exTextureInfo.width || _newTi.height != exTextureInfo.height))
			{
				_sprite.updateFlags |= exUpdateFlags.Vertex;
			}
			break;
		}
		if (_useTextureOffset)
		{
			_sprite.updateFlags |= exUpdateFlags.Vertex;
		}
		_sprite.updateFlags |= exUpdateFlags.UV;
		if (exTextureInfo == null || (object)exTextureInfo.texture != _newTi.texture)
		{
			_sprite.updateFlags |= exUpdateFlags.Vertex | exUpdateFlags.UV;
			(_sprite as exISprite).UpdateMaterial();
		}
	}
}
