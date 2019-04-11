class Hamburger
{
    public static Init()
    {
        $("#hamburgerMenu div").on("click", () =>
        {
            $("#hamburgerMenu").toggleClass("visibleHamburger");
        });
    }
}