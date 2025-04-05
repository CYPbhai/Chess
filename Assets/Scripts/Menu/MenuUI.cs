using UnityEngine;

public class MenuUI : MonoBehaviour
{
    protected MenuUI previousMenu;

    public virtual void Show(MenuUI previous)
    {
        if (previous)
            previousMenu = previous;
        gameObject.SetActive(true);
    }

    public virtual void Hide(bool returnToPreviousMenu)
    {
        gameObject.SetActive(false);

        if(returnToPreviousMenu)
            previousMenu.Show(null);
    }
}
