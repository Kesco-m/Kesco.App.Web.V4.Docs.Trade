// изменить картинку для image
function UpDownImage(e) {
    var imgSrc = e.target.src;
    if (contains(imgSrc, 'ScrollDownEnabled.gif'))
        e.target.src = '/styles/ScrollUpEnabled.gif'; 
    else
        e.target.src = '/styles/ScrollDownEnabled.gif';
}

// поменять картинку экспандера на показать
function upImageById(imgId) {
    var img = gi(imgId);
    if(img)
      img.src = '/styles/ScrollUpEnabled.gif';
}

// поменять картинку экспандера на скрыть
function downImageById(imgId) {
    var img = gi(imgId);
    if (img)
        img.src = '/styles/ScrollDownEnabled.gif';
}

// показать картинку экспандера
function ShowImageById(imgId) {
    var img = gi(imgId);
    if (img)
        img.style.display = 'inline';
}

// скрыть картинку экспандера
function HideImageById(imgId) {
    var img = gi(imgId);
    if (img)
        img.style.display = 'none';
}

// показать или скрыть содержимое экспандера
function showOrHideBlock(blockId, action) {
    var bl = gi(blockId);

    if (bl)
        bl.style.display = action;
}

// содержит ли элемент подстроку
function contains(r, s) {
    return r.indexOf(s) !== -1;
}