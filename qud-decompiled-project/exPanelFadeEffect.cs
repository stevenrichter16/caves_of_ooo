using UnityEngine;

public class exPanelFadeEffect : MonoBehaviour
{
	private exUIPanel panel;

	private exSpriteColorController colorCtrl;

	private bool inited;

	private void Awake()
	{
		Init();
	}

	public void Init()
	{
		if (inited)
		{
			return;
		}
		panel = GetComponent<exUIPanel>();
		colorCtrl = GetComponent<exSpriteColorController>();
		if ((bool)panel)
		{
			panel.AddEventListener("onStartFadeIn", delegate
			{
				panel.gameObject.SetActive(value: true);
				if ((bool)colorCtrl)
				{
					colorCtrl.color = new Color(1f, 1f, 1f, 0f);
				}
			});
			panel.AddEventListener("onFinishFadeOut", delegate
			{
				panel.gameObject.SetActive(value: false);
			});
			panel.AddEventListener("onFadeIn", delegate(exUIEvent _event)
			{
				exUIRatioEvent exUIRatioEvent2 = _event as exUIRatioEvent;
				if ((bool)colorCtrl)
				{
					colorCtrl.color = new Color(1f, 1f, 1f, exUIRatioEvent2.ratio);
				}
			});
			panel.AddEventListener("onFadeOut", delegate(exUIEvent _event)
			{
				exUIRatioEvent exUIRatioEvent2 = _event as exUIRatioEvent;
				if ((bool)colorCtrl)
				{
					colorCtrl.color = new Color(1f, 1f, 1f, 1f - exUIRatioEvent2.ratio);
				}
			});
		}
		inited = true;
	}
}
