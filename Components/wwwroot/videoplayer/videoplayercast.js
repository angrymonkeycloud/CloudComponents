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
    return new Promise(function (resolve, reject) {
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
    });
}
export function init() {
    return __awaiter(this, void 0, void 0, function () {
        var _this = this;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0:
                    console.log(cjs);
                    console.log(1);
                    return [4 /*yield*/, loadJs(castJsUrl)];
                case 1:
                    _a.sent();
                    console.log(2);
                    return [2 /*return*/, new Promise(function (resolve, reject) {
                            if (!cjs)
                                cjs = new Castjs();
                            console.log(3);
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
                                            if (log)
                                                console.log(4);
                                            if (log)
                                                console.log(e);
                                            if (log)
                                                console.log(5);
                                            if (e === 'available') {
                                                console.log(6);
                                                resolve(null);
                                                return [2 /*return*/];
                                            }
                                            if (log)
                                                console.log(7);
                                            if (firstCasting && e === 'playing') {
                                                if (castPosition !== null) {
                                                    cjs.seek(castPosition);
                                                    castPosition = null;
                                                }
                                                firstCasting = false;
                                            }
                                            if (log)
                                                console.log(8);
                                            return [4 /*yield*/, DotNet.invokeMethodAsync('AngryMonkey.Cloud.Components', 'HandleCastJsEventStatic', e, additionalValue)];
                                        case 1:
                                            _a.sent();
                                            if (log)
                                                console.log(9);
                                            return [2 /*return*/];
                                    }
                                });
                            }); });
                            console.log(4.1);
                            cjs.on('error', function (e) {
                                console.log('error: ' + e);
                                reject(e);
                            });
                            console.log(4.2);
                        })];
            }
        });
    });
}
export function startCasting(url, position) {
    return __awaiter(this, void 0, void 0, function () {
        var alreadyCasting;
        return __generator(this, function (_a) {
            console.log(100);
            console.log(cjs);
            alreadyCasting = cjs.connected && cjs.src === url;
            console.log(101);
            if (!alreadyCasting) {
                firstCasting = true;
                console.log(102);
                cjs.cast(url, {
                    title: 'Coverbox TV',
                });
                console.log(103);
            }
            if (position !== undefined) {
                if (firstCasting)
                    castPosition = position;
                else
                    cjs.seek(position);
            }
            console.log(104);
            cjs.play();
            console.log(105);
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
