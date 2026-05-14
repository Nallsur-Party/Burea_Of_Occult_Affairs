using UnityEngine;
using UnityEngine.Events;

public class UIDiaryPageController : MonoBehaviour
{
    [SerializeField] private Transform pagesRoot;
    [SerializeField] private int startPageIndex;
    [SerializeField] private bool clampIndex = true;
    [SerializeField] private UnityEvent<int> onPageChanged;

    private GameObject[] pages;
    private int currentPageIndex = -1;

    public int CurrentPageIndex => currentPageIndex;
    public int PageCount => pages != null ? pages.Length : 0;

    private void Awake()
    {
        CollectPages();

        if (pages == null || pages.Length == 0)
        {
            return;
        }

        ShowPage(startPageIndex);
    }

    [ContextMenu("Refresh Pages")]
    public void CollectPages()
    {
        Transform root = pagesRoot != null ? pagesRoot : transform;
        int childCount = root.childCount;
        pages = new GameObject[childCount];

        for (int i = 0; i < childCount; i++)
        {
            pages[i] = root.GetChild(i).gameObject;
        }
    }

    [ContextMenu("Next Page")]
    public void ShowNextPage()
    {
        if (PageCount == 0)
        {
            return;
        }

        ShowPage(currentPageIndex + 1);
    }

    [ContextMenu("Previous Page")]
    public void ShowPreviousPage()
    {
        if (PageCount == 0)
        {
            return;
        }

        ShowPage(currentPageIndex - 1);
    }

    public void ShowPage(int pageIndex)
    {
        if (PageCount == 0)
        {
            currentPageIndex = -1;
            return;
        }

        int resolvedIndex = ResolvePageIndex(pageIndex);
        if (resolvedIndex < 0 || resolvedIndex >= PageCount)
        {
            return;
        }

        currentPageIndex = resolvedIndex;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == currentPageIndex);
            }
        }

        onPageChanged.Invoke(currentPageIndex);
    }

    public void ShowFirstPage()
    {
        ShowPage(0);
    }

    public void ShowLastPage()
    {
        if (PageCount == 0)
        {
            return;
        }

        ShowPage(PageCount - 1);
    }

    private int ResolvePageIndex(int pageIndex)
    {
        if (clampIndex)
        {
            return Mathf.Clamp(pageIndex, 0, PageCount - 1);
        }

        if (PageCount == 0)
        {
            return -1;
        }

        if (pageIndex < 0)
        {
            return PageCount - 1;
        }

        if (pageIndex >= PageCount)
        {
            return 0;
        }

        return pageIndex;
    }
}
