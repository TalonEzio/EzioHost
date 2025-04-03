export function initBeerSlider(id) {
    let beerSlider = new window.BeerSlider(document.getElementById(id));

}
export function initImageCompare(id, beforeImage, afterImage) {

    window.SlickImageCompare.destroyAll();

    const options = {
        beforeImage: beforeImage,
        afterImage: afterImage
    };
    const _ = new window.SlickImageCompare(`#${id}`, options);

    console.log('ok');
}