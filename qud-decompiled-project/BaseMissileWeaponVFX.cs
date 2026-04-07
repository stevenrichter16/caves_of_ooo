using UnityEngine;

[ExecuteInEditMode]
public class BaseMissileWeaponVFX : MonoBehaviour
{
	private MissileWeaponVFXConfiguration configuration;

	private void Start()
	{
	}

	public virtual void OnUpdate(float t, float duration)
	{
	}

	public virtual void configure(MissileWeaponVFXConfiguration configuration, int path, ref float duration)
	{
		this.configuration = configuration;
	}

	public virtual void start()
	{
	}

	public virtual void pool()
	{
		base.gameObject.SetActive(value: false);
		if (configuration != null)
		{
			configuration = null;
			PooledPrefabManager.Return(base.gameObject);
		}
	}
}
