#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include <inttypes.h>
#include "softfloat.h"

/* Test-only adapter to unmodified Berkeley SoftFloat 3e, ARM-VFPv2 specialization. */
int main(void) {
    char op[24]; uint32_t av,bv,cv,mode;
    const uint_fast8_t rounding[] = {softfloat_round_near_even,softfloat_round_max,softfloat_round_min,softfloat_round_minMag};
    while (scanf("%23s %" SCNx32 " %" SCNx32 " %" SCNx32 " %" SCNu32,op,&av,&bv,&cv,&mode)==5) {
        float32_t a={av}, b={bv}, c={cv}, result={0}; uint32_t value=0;
        softfloat_roundingMode=rounding[mode]; softfloat_detectTininess=softfloat_tininess_beforeRounding; softfloat_exceptionFlags=0;
        if (!strcmp(op,"ADD")) result=f32_add(a,b);
        else if (!strcmp(op,"SUB")) result=f32_sub(a,b);
        else if (!strcmp(op,"MUL")) result=f32_mul(a,b);
        else if (!strcmp(op,"DIV")) result=f32_div(a,b);
        else if (!strcmp(op,"SQRT")) result=f32_sqrt(a);
        else if (!strcmp(op,"FMA")) result=f32_mulAdd(a,b,c);
        else if (!strcmp(op,"I32")) value=(uint32_t)f32_to_i32(a,rounding[mode],true);
        else if (!strcmp(op,"U32")) value=(uint32_t)f32_to_ui32(a,rounding[mode],true);
        else if (!strcmp(op,"FROM_I32")) result=i32_to_f32((int32_t)av);
        else if (!strcmp(op,"FROM_U32")) result=ui32_to_f32(av);
        else if (!strcmp(op,"HALF")) value=f32_to_f16(a).v;
        else if (!strcmp(op,"FROM_HALF")) {float16_t half={(uint16_t)av}; result=f16_to_f32(half);}
        else return 2;
        if (strcmp(op,"I32")&&strcmp(op,"U32")&&strcmp(op,"HALF")) value=result.v;
        uint32_t f=softfloat_exceptionFlags;
        uint32_t flags=((f&softfloat_flag_invalid)?1:0)|((f&softfloat_flag_infinite)?2:0)|((f&softfloat_flag_overflow)?4:0)|((f&softfloat_flag_underflow)?8:0)|((f&softfloat_flag_inexact)?16:0);
        printf("%" PRIu32 " %" PRIu32 "\n",value,flags);
    }
    return 0;
}
