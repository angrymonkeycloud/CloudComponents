var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
var cjs;
var firstCasting = true;
var castPosition = null;
var castJsUrl = 'https://cdnjs.cloudflare.com/ajax/libs/castjs/5.2.0/cast.min.js';
function loadJs(sourceUrl) {
    return __awaiter(this, void 0, void 0, function () {
        return __generator(this, function (_a) {
            return [2 /*return*/, new Promise(function (resolve, reject) {
                    var scripts = document.getElementsByTagName('script');
                    for (var i = 0; i < scripts.length; i++)
                        if (scripts[i].src == sourceUrl) {
                            resolve(null);
                            return;
                        }
                    var tag = document.createElement('script');
                    tag.src = sourceUrl;
                    tag.type = "text/javascript";
                    tag.onload = function () {
                        resolve(null);
                    };
                    tag.onerror = function () {
                        console.error("Failed to load script: " + sourceUrl);
                        reject("Failed to load script");
                    };
                    document.body.appendChild(tag);
                })];
        });
    });
}
export function createCastJsInstance() {
    return __awaiter(this, void 0, void 0, function () {
        return __generator(this, function (_a) {
            return [2 /*return*/, new window.Castjs()];
        });
    });
}
export function init() {
    return __awaiter(this, void 0, void 0, function () {
        var _this = this;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0:
                    console.log('init: start');
                    console.log(cjs);
                    return [4 /*yield*/, loadJs(castJsUrl)];
                case 1:
                    _a.sent();
                    console.log('init: js loaded');
                    if (!cjs)
                        cjs = new Castjs();
                    return [2 /*return*/, new Promise(function (resolve, reject) {
                            console.log('init: before event');
                            cjs.on('event', function (e) { return __awaiter(_this, void 0, void 0, function () {
                                var additionalValue, log;
                                return __generator(this, function (_a) {
                                    switch (_a.label) {
                                        case 0:
                                            additionalValue = null;
                                            log = true;
                                            if (e === 'timeupdate') {
                                                additionalValue = cjs.time;
                                                log = false;
                                            }
                                            if (e === 'connect') {
                                                additionalValue = cjs.device;
                                            }
                                            if (log) {
                                                console.log('event: started');
                                                console.log('----------------');
                                                console.log(e);
                                                console.log('----------------');
                                            }
                                            if (e === 'available') {
                                                console.log('event: available');
                                                resolve(null);
                                                return [2 /*return*/];
                                            }
                                            //if (firstCasting && e === 'playing') {
                                            //	if (castPosition !== null) {
                                            //		cjs.seek(castPosition);
                                            //		castPosition = null;
                                            //	}
                                            //	firstCasting = false;
                                            //}
                                            return [4 /*yield*/, DotNet.invokeMethodAsync('AngryMonkey.Cloud.Components', 'HandleCastJsEventStatic', e, additionalValue)];
                                        case 1:
                                            //if (firstCasting && e === 'playing') {
                                            //	if (castPosition !== null) {
                                            //		cjs.seek(castPosition);
                                            //		castPosition = null;
                                            //	}
                                            //	firstCasting = false;
                                            //}
                                            _a.sent();
                                            if (log)
                                                console.log('event: ended');
                                            return [2 /*return*/];
                                    }
                                });
                            }); });
                            console.log('event: before error');
                            cjs.on('error', function (e) {
                                console.log('error: ' + e);
                                reject(e);
                            });
                            if (cjs.available)
                                resolve(null);
                            console.log('init: end');
                        })];
            }
        });
    });
}
export function startCasting(url, title, position) {
    return __awaiter(this, void 0, void 0, function () {
        var alreadyCasting;
        return __generator(this, function (_a) {
            console.log('casting: started');
            console.log(cjs);
            alreadyCasting = cjs.connected && cjs.src === url;
            console.log('casting: already casting = ' + alreadyCasting);
            if (!alreadyCasting) {
                firstCasting = true;
                console.log('casting: cast url');
                cjs.cast(url, {
                    title: title,
                });
                console.log('casting: cast url end');
            }
            if (position !== undefined) {
                if (firstCasting)
                    castPosition = position;
                else
                    cjs.seek(position);
            }
            console.log('casting: before play');
            console.log('casting: Available = ' + cjs.available);
            console.log('casting: Connected = ' + cjs.connected);
            console.log(cjs);
            console.log('_____________________________________');
            cjs.play();
            console.log('casting: after play');
            console.log('casting: Available = ' + cjs.available);
            console.log('casting: Connected = ' + cjs.connected);
            console.log(cjs);
            console.log('_____________________________________');
            console.log('casting: end');
            return [2 /*return*/];
        });
    });
}
export function stopCasting() {
    if (cjs.available)
        cjs.disconnect();
    //cjs = null;
    //var scripts = document.getElementsByTagName('script');
    //for (var i = 0; i < scripts.length; i++)
    //	if (scripts[i].src === castJsUrl) {
    //		scripts[i].parentNode.removeChild(scripts[i]);
    //		return;
    //	}
}
export function pauseCasting() {
    if (cjs.available)
        cjs.pause();
}
export function playCasting() {
    if (cjs.available)
        cjs.play();
}
