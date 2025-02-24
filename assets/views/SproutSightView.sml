<lane orientation="vertical" horizontal-content-alignment="middle" >
    <banner text="SproutSight Pro"  background={@Mods/StardewUI/Sprites/BannerBackground} background-border-thickness="48,0" padding="12" /> 
    <lane>

        <lane layout="150px content"
              margin="0, 16, 0, 0"
              orientation="vertical"
              horizontal-content-alignment="end"
              z-index="2">
            <frame *repeat={AllTabs}
                   layout="120px 64px"
                   margin={Margin}
                   padding="16, 0"
                   horizontal-content-alignment="middle"
                   vertical-content-alignment="middle"
                   background={@Mods/24v.SproutSight/Sprites/MenuTiles:TabButton}
                   focusable="true"
                   click=|^SelectTab(Value)|>
                <label text={Value} />
            </frame>
        </lane>

        <frame *switch={SelectedTab}
               layout="820px 500px"
               margin="0, 16, 0, 0"
               padding="32, 24"
               background={@Mods/StardewUI/Sprites/ControlBorder}>

            <lane *case="Today">
                <scrollable peeking="128">
                    <lane>
                        <lane orientation="vertical">
                            <lane>
                                <label text={TotalProceeds}/>
                                <image layout="24px" margin="5,0,0,10" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                            </lane>
                            <label *!if={ShippedSomething} margin="0,20,0,0" text="You have not shipped anything today." />
                            <lane *repeat={CurrentItems} tooltip={Name} vertical-content-alignment="middle">
                                <panel *switch={QualityName} layout="64px" vertical-content-alignment="end">
                                    <image layout="stretch" margin="4" sprite={Sprite} />
                                    <lane vertical-content-alignment="end">
                                        <image *case="Silver" layout="24px" margin="4" sprite={@Mods/24v.SproutSight/Sprites/Cursors:QualityStarSilver} />
                                        <image *case="Gold" layout="24px" margin="4" sprite={@Mods/24v.SproutSight/Sprites/Cursors:QualityStarGold} />
                                        <image *case="Iridium" layout="24px" margin="4" sprite={@Mods/24v.SproutSight/Sprites/Cursors:QualityStarIridium} />
                                        <spacer layout="stretch 0px" />
                                        <digits layout="content" scale="3.0" number={StackCount} />
                                    </lane>
                                </panel>
                                <lane vertical-content-alignment="middle">
                                    <label text={TotalSalePrice} margin="10,0,5,0"/>
                                    <image layout="24px" margin="0,0,10,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                                    <label text={FormattedSale}/>
                                </lane>
                            </lane>
                        </lane>
                        <lane orientation="vertical" margin="100,0,0,0">
                            <lane>
                                <label text="Other Spending"/>
                                <image layout="24px" margin="5,0,0,10" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                            </lane>
                            <lane layout="content 64px">
                                <label text="In:" margin="10,20,0,0"/>
                                <label text={TodayGoldIn} margin="10,20,0,0"/>
                                <label text="g" margin="0,20,0,0"/>
                            </lane>
                            <lane>
                                <label text="Out:" margin="10,20,0,0"/>
                                <label text={TodayGoldOut} color="Red" margin="10,20,0,0"/>
                                <label text="g" margin="0,20,0,10"/>
                            </lane>
                        </lane>
                    </lane>
                </scrollable>
            </lane>

            <lane *case="Totals" orientation="vertical" >
               <lane layout="820px 40px" horizontal-content-alignment="end" >
                    <label text="Current Date: " />
                    <label text={:Date} margin="0,0,0,20"/>
                </lane>
                <grid *context={TrackedData} layout="816px content" item-layout="count: 6" item-spacing="8,8" margin="0,10,10,10">
                    <frame layout="136px 64px" background={@Mods/StardewUI/Sprites/ButtonLight} vertical-content-alignment="middle" horizontal-content-alignment="middle">
                        <label text="Year" margin="16"/>
                    </frame>
                    <frame layout="136px 64px" background={@Mods/StardewUI/Sprites/ButtonLight} vertical-content-alignment="middle" horizontal-content-alignment="middle">
                        <label text="Spring" margin="16"/>
                    </frame>
                    <frame layout="136px 64px" background={@Mods/StardewUI/Sprites/ButtonLight} vertical-content-alignment="middle" horizontal-content-alignment="middle">
                        <label text="Summer" margin="16"/>
                    </frame>
                    <frame layout="136px 64px" background={@Mods/StardewUI/Sprites/ButtonLight} vertical-content-alignment="middle" horizontal-content-alignment="middle">
                        <label text="Fall" margin="16"/>
                    </frame>
                    <frame layout="136px 64px" background={@Mods/StardewUI/Sprites/ButtonLight} vertical-content-alignment="middle" horizontal-content-alignment="middle">
                        <label text="Winter" margin="16"/>
                    </frame>
                    <frame layout="136px 64px" background={@Mods/StardewUI/Sprites/ButtonLight} vertical-content-alignment="middle" horizontal-content-alignment="middle">
                        <lane vertical-content-alignment="middle" horizontal-content-alignment="middle">
                            <label text="Total" />
                            <image layout="24px" margin="5,0,0,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                        </lane>
                    </frame>
                    <lane *repeat={SeasonGrid} layout="136px 24px"  vertical-content-alignment="middle" horizontal-content-alignment="middle">
                        <label text={this} />
                    </lane>
                </grid>
            </lane>
           <lane *case="Day" layout="820px content" orientation="vertical">
                <scrollable peeking="128">
                    <lane *context={TrackedData} layout="816px content" orientation="vertical" >
                        <lane *repeat={DayGrid} orientation="vertical" margin="0,0,0,40">
                            <!-- Each Year -->
                            <!-- Item1=Year, Item2=List<(Season, List<GridElement>)> -->

                            <lane *repeat={Item2} vertical-content-alignment="end" margin="0,0,0,10"> 
                                <!-- Each season -->
                                <!-- Item1=Season Item2=List<GridElement>) -->

                                <lane *context={Item1} layout="140px 60px" vertical-content-alignment="end" >
                                    <image *if={IsSpring} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Spring} />
                                    <image *if={IsSummer} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Summer} />
                                    <image *if={IsFall} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Fall} />
                                    <image *if={IsWinter} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Winter} />
                                    <label text={Text} margin="5,0,0,0"/>
                                </lane>
                                <image *repeat={Item2} *if={IsSpring} tint="Green" fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/24v.SproutSight/Sprites/Cursors:Bar} />
                                <image *repeat={Item2} *if={IsSummer} tint="Yellow" fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/24v.SproutSight/Sprites/Cursors:Bar} />
                                <image *repeat={Item2} *if={IsFall} tint="Brown" fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/24v.SproutSight/Sprites/Cursors:Bar} />
                                <image *repeat={Item2} *if={IsWinter} tint="Blue" fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/24v.SproutSight/Sprites/Cursors:Bar} />
                                <lane *context={Item1} horizontal-content-alignment="end" layout="stretch content">
                                    <lane *if={IsWinter} layout="content stretch" vertical-content-alignment="start">
                                        <label text="Y-" />
                                        <label text={^^Item1}/>
                                        <image layout="24px" margin="5,1,0,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                                        </lane>
                                </lane>
                            </lane>
                        </lane>
                    </lane>
                </scrollable>
           </lane> 

           <lane *case="CashFlow" layout="820px content" orientation="vertical">
                <scrollable peeking="128">
                    <lane *context={TrackedData} layout="816px content" orientation="vertical" >

                        <lane *repeat={DayGrid} orientation="vertical" margin="0,0,0,40">
                            <!-- Each Year -->
                            <!-- Item1=Year, Item2=List<(Season, List<GridElement>)> -->

                            <lane *repeat={Item2} orientation="vertical"  margin="0,0,0,10"> 
                                <!-- Each season -->
                                <!-- Item1=Season Item2=List<GridElement>) -->

                                <lane vertical-content-alignment="end">
                                    <lane *context={Item1} layout="140px 60px" vertical-content-alignment="end" >
                                        <image *if={IsSpring} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Spring} />
                                        <image *if={IsSummer} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Summer} />
                                        <image *if={IsFall} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Fall} />
                                        <image *if={IsWinter} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Winter} />
                                        <label text={Text} margin="5,0,0,0"/>
                                    </lane>
                                    <image *repeat={Item2} *if={IsSpring} tint="Green" fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/24v.SproutSight/Sprites/Cursors:Bar} />
                                    <image *repeat={Item2} *if={IsSummer} tint="Yellow" fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/24v.SproutSight/Sprites/Cursors:Bar} />
                                    <image *repeat={Item2} *if={IsFall} tint="Brown" fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/24v.SproutSight/Sprites/Cursors:Bar} />
                                    <image *repeat={Item2} *if={IsWinter} tint="Blue" fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/24v.SproutSight/Sprites/Cursors:Bar} />
                                    <lane *context={Item1} horizontal-content-alignment="end" layout="stretch content">
                                        <lane *if={isWinter} layout="content stretch" vertical-content-alignment="start">
                                            <label text="Y-" />
                                            <label text={^^Item1}/>
                                            <image layout="24px" margin="5,1,0,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                                            </lane>
                                    </lane>
                                </lane>

                                <lane>
                                    <lane *context={Item1} layout="140px 0px" vertical-content-alignment="end" > </lane>
                                    <image *repeat={Item2} tint="Blue" fit="Stretch" margin="1,0,0,0" layout="20px 100px" tooltip={Tooltip} sprite={@Mods/24v.SproutSight/Sprites/Cursors:Bar} />
                                </lane>

                            </lane>
                        </lane>
                    </lane>
                </scrollable>
           </lane> 
        </frame>
    </lane>
</lane>

